namespace AgentScope.Harness.Subagent.Protocol;

public enum RemoteEventType
{
    RunStarted,
    RunFinished,
    RunError,
    TextDelta,
    ThinkingDelta,
    ToolCallStart,
    ToolCallEnd,
    ToolResult,
    RequireConfirm,
    Status,
    AgentEvent
}
