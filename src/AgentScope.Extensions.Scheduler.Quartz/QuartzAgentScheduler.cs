using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using Quartz;
using Quartz.Impl;

namespace AgentScope.Extensions.Scheduler.Quartz;

/// <summary>
/// Quartz 调度器实现。对标 Java QuartzAgentScheduler。
/// 使用 Quartz.NET 替代 Java Quartz。
/// </summary>
public sealed class QuartzAgentScheduler : IAgentScheduler, IDisposable
{
    private readonly IScheduler _scheduler;
    private readonly Dictionary<string, JobKey> _jobKeys = new();
    private readonly Func<string, IAgent>? _agentResolver;

    public QuartzAgentScheduler(Func<string, IAgent>? agentResolver = null)
    {
        _agentResolver = agentResolver;
        var factory = new StdSchedulerFactory();
        _scheduler = factory.GetScheduler().Result;
        _scheduler.Start();
    }

    public async Task<string> ScheduleAsync(ScheduleAgentTask task, CancellationToken ct = default)
    {
        var jobKey = new JobKey(task.TaskId, "agents");
        var job = JobBuilder.Create<AgentJob>()
            .WithIdentity(jobKey)
            .UsingJobData("agentName", task.AgentName)
            .UsingJobData("taskId", task.TaskId)
            .UsingJobData("inputJson", System.Text.Json.JsonSerializer.Serialize(task.InputParams ?? new Dictionary<string, object>()))
            .Build();

        // 若提供了解析器，把 Agent 实例直接放入 JobDataMap（RAMJobStore 无需序列化）
        if (_agentResolver != null)
            job.JobDataMap["agent"] = _agentResolver(task.AgentName);

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trigger-{task.TaskId}", "agents")
            .WithCronSchedule(task.CronExpression)
            .Build();

        await _scheduler.ScheduleJob(job, trigger, ct);
        _jobKeys[task.TaskId] = jobKey;
        return task.TaskId;
    }

    public async Task CancelAsync(string taskId, CancellationToken ct = default)
    {
        if (_jobKeys.TryGetValue(taskId, out var key))
        {
            await _scheduler.DeleteJob(key, ct);
            _jobKeys.Remove(taskId);
        }
    }

    public Task<IReadOnlyList<ScheduleAgentTask>> ListTasksAsync(CancellationToken ct = default)
    {
        var tasks = _jobKeys.Keys.Select(id => new ScheduleAgentTask(id, "", "")).ToList();
        return Task.FromResult<IReadOnlyList<ScheduleAgentTask>>(tasks);
    }

    public void Dispose()
    {
        _scheduler.Shutdown(true).Wait();
    }

    private sealed class AgentJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var agentName = context.JobDetail.JobDataMap.GetString("agentName") ?? "unknown";

            // 优先取注入的 Agent 实例，真正执行任务
            var agent = context.JobDetail.JobDataMap["agent"] as IAgent;
            if (agent == null)
            {
                await Console.Out.WriteLineAsync($"[Scheduler] 未找到 Agent '{agentName}'，跳过执行");
                return;
            }

            var inputJson = context.JobDetail.JobDataMap.GetString("inputJson");
            var inputMsg = BuildInputMsg(inputJson);

            try
            {
                if (inputMsg != null)
                    await agent.CallAsync(inputMsg);
                else
                    await agent.CallAsync(Array.Empty<Msg>());
            }
            catch (System.Exception ex)
            {
                await Console.Error.WriteLineAsync($"[Scheduler] 执行任务 '{agentName}' 失败: {ex.Message}");
            }
        }

        private static Msg? BuildInputMsg(string? inputJson)
        {
            if (string.IsNullOrWhiteSpace(inputJson)) return null;
            try
            {
                var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(inputJson);
                if (dict == null) return null;
                var text = dict.TryGetValue("text", out var t) ? t?.ToString()
                    : dict.TryGetValue("message", out var m) ? m?.ToString()
                    : null;
                return string.IsNullOrWhiteSpace(text)
                    ? null
                    : Msg.Builder().Role("user").TextContent(text).Build();
            }
            catch
            {
                return null;
            }
        }
    }
}
