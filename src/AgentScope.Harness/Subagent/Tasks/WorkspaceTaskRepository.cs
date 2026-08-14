using System.Collections.Concurrent;
using AgentScope.Core.Agent;

namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>基于工作区的任务仓库实现，对�?Java WorkspaceTaskRepository</summary>
public sealed class WorkspaceTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<string, BackgroundTask> _localTasks = new();
    private readonly ConcurrentDictionary<string, string> _localTaskSessionIds = new();
    private readonly ConcurrentDictionary<string, TaskRecord> _records = new();

    public BackgroundTask? GetTask(RuntimeContext? rc, string sessionId, string taskId)
    {
        _localTasks.TryGetValue(taskId, out var task);
        return task;
    }

    public BackgroundTask PutTask(RuntimeContext? rc, string taskId,
        string subAgentId, string sessionId, TaskRunSpec spec)
    {
        var task = spec switch
        {
            LocalTaskRunSpec local => new BackgroundTask(taskId, subAgentId,
                Task.Run(local.Execution)),
            RemoteTaskRunSpec remote => CreateRemoteTask(taskId, subAgentId, remote),
            AdoptedTaskRunSpec adopted => new BackgroundTask(taskId, subAgentId,
                adopted.Future),
            _ => throw new ArgumentException($"Unknown TaskRunSpec: {spec.GetType()}")
        };

        _localTasks[taskId] = task;
        _localTaskSessionIds[taskId] = sessionId;
        _records[taskId] = new TaskRecord(taskId, subAgentId)
        {
            Status = TaskStatus.Running,
            TransportType = spec is RemoteTaskRunSpec ? "agent-protocol" : "local"
        };

        return task;
    }

    public ICollection<BackgroundTask> ListTasks(RuntimeContext? rc,
        string sessionId, TaskStatus? filter = null)
    {
        var tasks = _localTasks.Values.AsEnumerable();
        if (filter.HasValue)
            tasks = tasks.Where(t => t.GetTaskStatus() == filter.Value);
        return tasks.ToList();
    }

    public bool CancelTask(RuntimeContext? rc, string sessionId, string taskId)
    {
        if (!_localTasks.TryGetValue(taskId, out var task)) return false;
        task.Cancel();
        if (_records.TryGetValue(taskId, out var record))
            record.Status = TaskStatus.Cancelled;
        return true;
    }

    private static BackgroundTask CreateRemoteTask(string taskId,
        string agentId, RemoteTaskRunSpec spec)
    {
        var tcs = new TaskCompletionSource<string?>();
        var bgTask = new BackgroundTask(taskId, agentId, tcs.Task);

        _ = Task.Run(async () =>
        {
            try
            {
                var client = new AgentProtocolTaskClient();
                await client.SubmitTaskAsync(spec.BaseUrl, spec.Headers,
                    taskId, spec.AgentId, spec.Input, spec.Context);
                var result = await client.WaitForResultAsync(spec.BaseUrl,
                    spec.Headers, taskId, 300);
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return bgTask;
    }

    public void Shutdown()
    {
        foreach (var task in _localTasks.Values)
            task.Cancel();
        _localTasks.Clear();
        _localTaskSessionIds.Clear();
        _records.Clear();
    }
}

