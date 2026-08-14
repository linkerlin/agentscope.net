namespace AgentScope.Extensions.Scheduler;

/// <summary>
/// 调度器接口。对标 Java AgentScheduler。
/// </summary>
public interface IAgentScheduler
{
    Task<string> ScheduleAsync(ScheduleAgentTask task, CancellationToken ct = default);
    Task CancelAsync(string taskId, CancellationToken ct = default);
    Task<IReadOnlyList<ScheduleAgentTask>> ListTasksAsync(CancellationToken ct = default);
}

public sealed record ScheduleAgentTask(
    string TaskId,
    string AgentName,
    string CronExpression,
    IDictionary<string, object>? InputParams = null);
