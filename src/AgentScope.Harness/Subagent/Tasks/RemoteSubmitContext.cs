namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>远程提交上下文，对应 Java RemoteSubmitContext</summary>
public sealed record RemoteSubmitContext
{
    public string? UserId { get; init; }
    public string? ParentSessionId { get; init; }
    public bool Stream { get; init; }
    public string? Detail { get; init; }
    public Dictionary<string, object>? Attributes { get; init; }

    public static RemoteSubmitContext Empty => new();
}

