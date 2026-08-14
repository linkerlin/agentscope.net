using AgentScope.Core.Events;

namespace AgentScope.Harness.Subagent.Protocol;

/// <summary>??????????? Java RemoteEventCodec</summary>
public static class RemoteEventCodec
{
    public static RemoteAgentEvent? FromAgentEvent(Event agentEvent)
    {
        var type = agentEvent.Type switch
        {
            EventType.ReasoningStart => RemoteEventType.RunStarted,
            EventType.ReasoningFinish => RemoteEventType.RunFinished,
            EventType.ToolCallStart => RemoteEventType.ToolCallStart,
            EventType.ToolCallFinish => RemoteEventType.ToolCallEnd,
            _ => RemoteEventType.AgentEvent
        };

        return new RemoteAgentEvent
        {
            Type = type,
            Text = agentEvent.Message?.GetTextContent(),
            Status = agentEvent.IsLast ? "completed" : "running",
            Timestamp = DateTime.UtcNow.ToString("O")
        };
    }
}
