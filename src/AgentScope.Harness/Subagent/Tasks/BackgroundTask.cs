namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>后台任务跟踪，对�?Java BackgroundTask</summary>
public sealed class BackgroundTask
{
    public string TaskId { get; }
    public string AgentId { get; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime LastCheckedAt { get; private set; } = DateTime.UtcNow;
    public bool Cancelled { get; private set; }
    public TaskCompletionSource<string?> Completion { get; } = new();

    private readonly Task<string?> _task;

    public BackgroundTask(string taskId, string agentId, Task<string?> task)
    {
        TaskId = taskId;
        AgentId = agentId;
        _task = task;
        _ = WireCompletionAsync();
    }

    private async Task WireCompletionAsync()
    {
        try
        {
            var result = await _task;
            Completion.TrySetResult(result);
        }
        catch (Exception ex)
        {
            Completion.TrySetException(ex);
        }
    }

    public void UpdateLastCheckedAt() => LastCheckedAt = DateTime.UtcNow;

    public TaskStatus GetTaskStatus()
    {
        if (Cancelled) return TaskStatus.Cancelled;
        if (_task.IsCompletedSuccessfully) return TaskStatus.Completed;
        if (_task.IsFaulted) return TaskStatus.Failed;
        if (_task.IsCanceled) return TaskStatus.Cancelled;
        return _task.IsCompleted ? TaskStatus.Completed : TaskStatus.Running;
    }

    public bool Cancel(bool mayInterruptIfRunning = true)
    {
        Cancelled = true;
        Completion.TrySetCanceled();
        return true;
    }
}

