using System.Text.Json.Serialization;

namespace AgentScope.Harness.Subagent.Protocol;

/// <summary>远程确认决策，对应 Java RemoteConfirmDecision</summary>
public sealed class RemoteConfirmDecision
{
    public string? ToolCallId { get; set; }
    public bool Approved { get; set; }

    public RemoteConfirmDecision() { }

    public RemoteConfirmDecision(string toolCallId, bool approved)
    {
        ToolCallId = toolCallId;
        Approved = approved;
    }
}
