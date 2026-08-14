using AgentScope.Harness.Subagent.Protocol;
namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>远程任务状态，对应 Java RemoteTaskStatus</summary>
public sealed record RemoteTaskStatus(
    string Status,
    string? Error = null,
    List<RemotePendingConfirm>? PendingConfirms = null)
{
    public bool IsAwaitingConfirm => Status == "awaiting_confirm";
    public bool IsTerminalSuccess => Status == "success";
    public bool IsTerminalFailure => Status is "error" or "failed";
    public bool IsCancelled => Status is "cancelled" or "canceled";
}

