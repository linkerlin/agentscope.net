namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>任务记录，对�?Java TaskRecord</summary>
public sealed class TaskRecord
{
    public string TaskId { get; set; }
    public string SubAgentId { get; set; }
    public string ParentAgentId { get; set; } = "";
    public string ParentSessionId { get; set; } = "";
    public string SubSessionId { get; set; } = "";
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public bool CancelRequested { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? TransportType { get; set; }
    public string? RemoteBaseUrl { get; set; }
    public bool AwaitingConfirm { get; set; }
    public long LastEventSeq { get; set; }

    public TaskRecord(string taskId, string subAgentId)
    {
        TaskId = taskId;
        SubAgentId = subAgentId;
    }

    public void Touch() => LastUpdatedAt = DateTime.UtcNow;
}

