using System.Text.Json.Serialization;

namespace AgentScope.Harness.Subagent.Protocol;

/// <summary>远程子代理事件 DTO，对应 Java RemoteAgentEvent</summary>
public sealed class RemoteAgentEvent
{
    public long Seq { get; set; }
    public RemoteEventType Type { get; set; }
    public string? TaskId { get; set; }
    public string? AgentId { get; set; }
    public string? Timestamp { get; set; }
    public string? Text { get; set; }
    public string? ToolCallId { get; set; }
    public string? ToolName { get; set; }
    public string? ToolInput { get; set; }
    public string? Status { get; set; }
    public string? Error { get; set; }
    public string? EventType { get; set; }
    public string? Payload { get; set; }
    public List<RemotePendingConfirm>? PendingConfirms { get; set; }
}
