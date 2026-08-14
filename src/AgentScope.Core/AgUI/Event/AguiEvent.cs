using AgentScope.Core.AgUI.Model;

namespace AgentScope.Core.AgUI.Event;

/// <summary>
/// AG-UI 事件类型枚举。对�?Java AguiEventType�?/// </summary>
public enum AguiEventType
{
    RunStarted, RunFinished, RunError,
    StepStarted, StepFinished,
    TextMessageStart, TextMessageContent, TextMessageEnd, TextMessageChunk,
    ToolCallStart, ToolCallArgs, ToolCallEnd, ToolCallChunk, ToolCallResult,
    StateSnapshot, StateDelta, MessagesSnapshot,
    ActivitySnapshot, ActivityDelta,
    Raw, Custom,
    ReasoningStart, ReasoningMessageStart, ReasoningMessageContent,
    ReasoningMessageEnd, ReasoningMessageChunk, ReasoningEnd, ReasoningEncryptedValue
}

/// <summary>
/// AG-UI 事件记录基类。对�?Java sealed interface AguiEvent�?/// </summary>
public abstract record AguiEvent(
    AguiEventType Type,
    string ThreadId,
    string RunId,
    long? Timestamp = null,
    object? RawEvent = null);

// ── Run 生命周期 ──
public sealed record RunStarted(string ThreadId, string RunId, string? ParentRunId, RunAgentInput Input,
    long? Timestamp = null) : AguiEvent(AguiEventType.RunStarted, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record RunFinished(string ThreadId, string RunId, RunFinishedOutcome Outcome,
    long? Timestamp = null) : AguiEvent(AguiEventType.RunFinished, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record RunError(string ThreadId, string RunId, string Message, int ErrorCode,
    long? Timestamp = null) : AguiEvent(AguiEventType.RunError, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

// ── 步骤 ──
public sealed record StepStarted(string ThreadId, string RunId, string StepName,
    long? Timestamp = null) : AguiEvent(AguiEventType.StepStarted, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record StepFinished(string ThreadId, string RunId, string StepName,
    long? Timestamp = null) : AguiEvent(AguiEventType.StepFinished, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

// ── 文本消杯�?──
public sealed record TextMessageStart(string ThreadId, string RunId, string MessageId, string Role,
    long? Timestamp = null) : AguiEvent(AguiEventType.TextMessageStart, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record TextMessageContent(string ThreadId, string RunId, string Delta,
    long? Timestamp = null) : AguiEvent(AguiEventType.TextMessageContent, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record TextMessageEnd(string ThreadId, string RunId,
    long? Timestamp = null) : AguiEvent(AguiEventType.TextMessageEnd, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

// ── 工具调用�?──
public sealed record ToolCallStart(string ThreadId, string RunId, string ToolCallId, string ToolCallName,
    long? Timestamp = null) : AguiEvent(AguiEventType.ToolCallStart, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record ToolCallArgs(string ThreadId, string RunId, string Delta,
    long? Timestamp = null) : AguiEvent(AguiEventType.ToolCallArgs, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record ToolCallEnd(string ThreadId, string RunId,
    long? Timestamp = null) : AguiEvent(AguiEventType.ToolCallEnd, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record ToolCallResult(string ThreadId, string RunId, string ToolName, object? Result,
    bool IsError = false, long? Timestamp = null) : AguiEvent(AguiEventType.ToolCallResult, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

// ── 推睆�?──
public sealed record ReasoningStart(string ThreadId, string RunId,
    long? Timestamp = null) : AguiEvent(AguiEventType.ReasoningStart, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record ReasoningMessageStart(string ThreadId, string RunId,
    long? Timestamp = null) : AguiEvent(AguiEventType.ReasoningMessageStart, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record ReasoningMessageContent(string ThreadId, string RunId, string Delta,
    long? Timestamp = null) : AguiEvent(AguiEventType.ReasoningMessageContent, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record ReasoningMessageEnd(string ThreadId, string RunId,
    long? Timestamp = null) : AguiEvent(AguiEventType.ReasoningMessageEnd, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record ReasoningEnd(string ThreadId, string RunId,
    long? Timestamp = null) : AguiEvent(AguiEventType.ReasoningEnd, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

// ── 快照与扩�?──
public sealed record StateSnapshot(string ThreadId, string RunId, object State,
    long? Timestamp = null) : AguiEvent(AguiEventType.StateSnapshot, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

public sealed record CustomEvent(string ThreadId, string RunId, string Name, object? Value = null,
    long? Timestamp = null) : AguiEvent(AguiEventType.Custom, ThreadId, RunId, Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

// ── Outcome 类型 ──
public abstract record RunFinishedOutcome;
public sealed record RunFinishedSuccessOutcome(object? Result) : RunFinishedOutcome;
public sealed record RunFinishedInterruptOutcome(Interrupt Interrupt) : RunFinishedOutcome;

/// <summary>
/// 挂起的中断（等待用户处睆）。对�?Java Interrupt�?/// </summary>
public sealed record Interrupt(string Reason, string? ReplyId = null, IDictionary<string, object>? Metadata = null);
