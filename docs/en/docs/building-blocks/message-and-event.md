---
title: "Message and Event"
description: "Msg / ContentBlock message model and Event / EventType streaming events"
---

## Message (Msg)

`Msg` (`AgentScope.Core.Message`) is the unified message type passed between agents and between agents and models.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Id` | `string` | Random GUID | Unique message identifier |
| `Name` | `string?` | null | Optional sender name |
| `Role` | `string` | `"user"` | Role: `system` / `user` / `assistant` / `tool` (same as `MsgRole` enum) |
| `Content` | `object?` | null | Content: string or `List<ContentBlock>` |
| `Url` | `List<string>?` | null | Additional URL list |
| `Timestamp` | `DateTime` | UtcNow | Creation time |
| `Metadata` | `Dictionary<string, object>?` | null | Extended metadata |

### Construction: Msg.Builder()

```csharp
using AgentScope.Core.Message;

Msg msg = Msg.Builder()
    .Id("msg-001")                       // Optional
    .Name("alice")                       // Optional
    .Role("user")                        // Default "user"
    .TextContent("Summarize this document for me")       // Equivalent to Content("plain text")
    .Url(new List<string>())             // Optional
    .Metadata(new Dictionary<string, object>())   // Or use AddMetadata(key, value) to add one by one
    .Build();
```

MsgBuilder all methods: `Id` / `Name` / `Role` / `Content(object)` / `TextContent(string)` / `Url` / `Timestamp` / `Metadata` / `AddMetadata` / `Build`.

### Convenience Subclasses

| Class | Construction | Description |
|-------|-------------|-------------|
| `UserMessage` | `new UserMessage()` or `new UserMessage(name, content)` | Role fixed to `"user"` (**no single-argument text constructor**; use Builder for text) |
| `SystemMessage` / `AssistantMessage` / `ToolResultMessage` | Same pattern | Role fixed to `system` / `assistant` / `tool` respectively |

### Reading Content

```csharp
string? text = msg.GetTextContent();   // Returns string directly; concatenates text blocks if block list
msg.SetTextContent("new content");      // Overwrites with plain text
string json = msg.ToString();          // JSON serialization
```

## ContentBlock System

Multimodal content is represented by `ContentBlock` records (same namespace), placed in `List<ContentBlock>` as `Msg.Content`. All blocks are constructed with record object initializers (no Builder):

| Block Type | `Type` Value | Required Fields | Description |
|------------|-------------|-----------------|-------------|
| `TextBlock` | `"text"` | `Text` | Text |
| `ImageBlock` | `"image"` | `Url` (or `Data` bytes) | Image |
| `AudioBlock` | `"audio"` | `Url`, optional `DurationSec`, `MimeType`, `Data` (bytes) | Audio |
| `VideoBlock` | `"video"` | `Url`, optional `PosterUrl`, `MimeType`, `Data` (bytes) | Video |
| `ToolUseBlock` | `"tool_use"` | `Id`, `Name`, optional `Input` | Model-initiated tool call |
| `ToolResultBlock` | `"tool_result"` | `Id`, optional `Output`, `IsError` | Tool execution result, `ExtractText()` extracts text |
| `ThinkingBlock` | `"thinking"` | `Thinking`, optional `Signature` | Model thinking process |

```csharp
var msg = Msg.Builder()
    .Role("user")
    .Content(new List<ContentBlock>
    {
        new TextBlock { Text = "What is in this picture?" },
        new ImageBlock { Url = "https://example.com/cat.png", MimeType = "image/png" }
    })
    .Build();
```

## Events (Event and EventType)

`StreamEventsAsync` produces `Event` (`AgentScope.Core.Events`, note the distinction from the fine-grained `AgentEvent` record hierarchy described below):

```csharp
public class Event
{
    public EventType Type { get; }                          // Event type
    public Msg? Message { get; }                            // Associated message (can be null)
    public bool IsLast { get; }                             // Whether this is the last event in the stream
    public IReadOnlyDictionary<string, object> Metadata { get; }

    // Convenience checks: IsReasoning / IsToolCall / IsActing / IsSummary / IsError
    public static Event ErrorEvent(Msg? message, string? errorMessage = null, bool isLast = true);
}
```

### EventType Enum

| Category | Enum Values |
|----------|-------------|
| Reasoning | `ReasoningStart` / `ReasoningChunk` / `ReasoningFinish` |
| Tool Call | `ToolCallStart` / `ToolCallChunk` / `ToolCallFinish` |
| Acting | `ActingStart` / `ActingChunk` / `ActingFinish` |
| Summary | `SummaryStart` / `SummaryChunk` / `SummaryFinish` |
| Error | `Error` |

### Consumption Example

```csharp
using AgentScope.Core.Events;

await foreach (Event evt in agent.StreamEventsAsync(userMsg))
{
    switch (evt.Type)
    {
        case EventType.ReasoningChunk:
            Console.Write(evt.Message?.GetTextContent());
            break;
        case EventType.ToolCallStart:
            Console.WriteLine("\n[Tool call started]");
            break;
        case EventType.Error:
            Console.WriteLine($"\n[Error] {evt.Metadata.GetValueOrDefault("error")}");
            break;
    }
    if (evt.IsLast) break;
}
```

## Fine-Grained AgentEvent Record Hierarchy

`AgentScope.Core.Events` also contains a set of fine-grained event records (common abstract base class `AgentEvent(string ReplyId)`), primarily used by protocol adaptation layers such as A2A / AgUI:

| Event | Payload | Description |
|-------|---------|-------------|
| `AgentStartEvent` / `AgentEndEvent` | `AgentName`, `SessionId?` | Agent lifecycle |
| `AgentResultEvent` | `Msg Result` | Final result |
| `TextBlockStartEvent` / `TextBlockDeltaEvent` / `TextBlockEndEvent` | `Text` (Delta) | Text block streaming |
| `ThinkingBlockStartEvent` / `ThinkingBlockDeltaEvent` / `ThinkingBlockEndEvent` | `Thinking` (Delta) | Thinking block streaming |
| `ToolCallEvent` / `ToolResultEvent` | `ToolUseBlock` / `ToolResultBlock` | Tool call and result |
| `RequireUserConfirmEvent` | `ToolName`, `Arguments?` | Requires user confirmation (HITL) |
| `ExceedMaxItersEvent` / `AllToolsDeniedEvent` | `MaxIterations` | Loop termination exception |
| `HintBlockEvent` | `Hint` | Non-interactive hint |
| `ModelCallStartEvent` / `ModelCallEndEvent` | `ModelName?` | Model call boundary |
| `CustomAgentEvent` | `Name`, `Value?` | Custom extension |

`Events/AdditionalEvents.cs` also contains: `UserConfirmResultEvent` (carries `ConfirmResult`), `RequestStopEvent`, `DataBlockStart/Delta/EndEvent` (binary data stream, Base64 Delta), `SubagentExposedEvent`, `RequireExternalExecutionEvent` / `ExternalExecutionResultEvent`.

:::{note}
`EnhancedReActAgent.StreamEventsAsync` produces the coarse-grained `Event` described in the previous section; the `AgentEvent` record hierarchy is used for protocol adaptation and more fine-grained UI event modeling. Both reside under the `AgentScope.Core.Events` namespace.
:::

## Related Documentation

- [Agent](./agent.md) — Who produces events and how to consume them
- [Model](./model.md) — `ChatResponse` streaming chunks in the model layer
