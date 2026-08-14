using System.Text.Json.Serialization;

namespace AgentScope.Harness.Subagent.Protocol;

/// <summary>远程待确认项，对应 Java RemotePendingConfirm</summary>
public sealed class RemotePendingConfirm
{
    public string? ToolCallId { get; set; }
    public string? ToolName { get; set; }
    public string? ToolInputJson { get; set; }

    public RemotePendingConfirm() { }

    public RemotePendingConfirm(string toolCallId, string toolName, string toolInputJson)
    {
        ToolCallId = toolCallId;
        ToolName = toolName;
        ToolInputJson = toolInputJson;
    }
}
