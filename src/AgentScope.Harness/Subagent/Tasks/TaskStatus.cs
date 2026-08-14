namespace AgentScope.Harness.Subagent.Tasks;

public enum TaskStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}

public static class TaskStatusExtensions
{
    public static bool IsTerminal(this TaskStatus status) =>
        status is TaskStatus.Completed or TaskStatus.Failed or TaskStatus.Cancelled;
}

