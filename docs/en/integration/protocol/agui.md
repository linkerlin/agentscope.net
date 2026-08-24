# AG-UI

The `AgentScope.Core.AgUI` namespace converts AgentScope stream events into [AG-UI Protocol](https://github.com/ag-ui/ag-ui) events so front-end UIs can render an Agent run in real time.

## When to use

- Connect an AgentScope Agent to an AG-UI-compatible front end or custom chat UI.
- Stream text, reasoning, tool calls, and other AG-UI events over SSE.

All classes live in `AgentScope.Core.AgUI` — no additional NuGet package required.

## Quickstart

```csharp
using AgentScope.Core.AgUI.Adapter;
using AgentScope.Core.AgUI.Model;

// Configure the adapter
var config = new AguiAdapterConfig
{
    EnableReasoning = true,
    EmitTokenUsage = false,
    EmitToolCallArgs = true,
    DefaultAgentId = "default"
};

// Create the adapter
var adapter = new AguiAgentAdapter(agent, config);

// Build input
var input = new RunAgentInput(
    ThreadId: "thread-1",
    RunId: "run-1",
    Messages: new[] { AguiMessage.UserMessage("Hello") });

// Consume the event stream (serialize as SSE for the front end)
await foreach (var evt in adapter.RunAsync(input))
{
    var sseData = AguiEventEncoder.Encode(evt);
    // Write to HTTP response stream
}
```

## Core API

### AguiAgentAdapter

| Constructor | Description |
| --- | --- |
| `AguiAgentAdapter(IAgent agent, AguiAdapterConfig? config = null)` | Wrap an Agent and convert to AG-UI event stream |

| Method | Description |
| --- | --- |
| `IAsyncEnumerable<AguiEvent> RunAsync(RunAgentInput input)` | Run the Agent and produce AG-UI events as an async stream |

### AguiAdapterConfig

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `ToolMergeMode` | `ToolMergeMode` | `FrontendOnly` | Tool merge strategy |
| `EmitStateEvents` | bool | `false` | Emit state snapshot/delta events |
| `EmitToolCallArgs` | bool | `true` | Emit tool call argument events |
| `EmitTokenUsage` | bool | `false` | Emit token usage information |
| `EnableReasoning` | bool | `true` | Enable reasoning/thinking events |
| `EmitRunFinishedAfterError` | bool | `true` | Emit `RunFinished` after error |
| `RunTimeout` | `TimeSpan?` | `null` | Per-run timeout |
| `DefaultAgentId` | string | `"default"` | Default agent identifier |
| `EmitSubagentEventsAsNative` | bool | `false` | Emit sub-agent events as native AG-UI events |

### AguiAgentRegistry

```csharp
var registry = new AguiAgentRegistry();
registry.Register("agent-1", myAgent);
registry.RegisterFactory("agent-2", () => new MyAgent());

var agent = registry.GetAgent("agent-1");
bool exists = registry.HasAgent("agent-1");
registry.Unregister("agent-1");
registry.Clear();
```

### AguiEventEncoder

| Method | Description |
| --- | --- |
| `Encode(AguiEvent)` | Encode as SSE: `data: {json}\n\n` |
| `EncodeToJson(AguiEvent)` | Return JSON string only |
| `EncodeComment(string)` | Encode SSE comment |
| `KeepAlive()` | Generate SSE keepalive signal |

### AguiMessageConverter

```csharp
var converter = new AguiMessageConverter();
var msg = converter.ToMsg(aguiMessage);
var aguiMsg = converter.ToAguiMessage(msg);
var msgs = converter.ToMsgList(runAgentInput);
```

### AguiToolConverter

```csharp
var tool = AguiToolConverter.ToAguiTool("search", "Search tool", schema);
```

### RunAgentInput

```csharp
public sealed record RunAgentInput(
    string ThreadId,
    string RunId,
    IReadOnlyList<AguiMessage> Messages,
    IReadOnlyList<AguiTool>? Tools = null,
    IReadOnlyList<AguiContext>? Context = null,
    IReadOnlyDictionary<string, object>? State = null,
    IReadOnlyDictionary<string, string>? ForwardedProps = null,
    IReadOnlyList<AguiResume>? Resume = null);
```

Static factory methods on `AguiMessage`: `UserMessage`, `AssistantMessage`, `SystemMessage`, `ToolMessage`.

### Event Mapping

| AgentScope Event | AG-UI Event |
| --- | --- |
| `ActingStart` | `TextMessageStart` |
| `ActingChunk` | `TextMessageContent` |
| `ActingFinish` | `TextMessageEnd` |
| `ToolCallStart` | `ToolCallStart` |
| `ToolCallChunk` | `ToolCallArgs` |
| `ToolCallFinish` | `ToolCallEnd` |
| `ReasoningStart` (requires `EnableReasoning`) | `ReasoningStart` / `ReasoningMessageStart` |
| `ReasoningChunk` | `ReasoningMessageContent` |
| `ReasoningFinish` | `ReasoningMessageEnd` / `ReasoningEnd` |

## Tool Merge Modes

| `ToolMergeMode` | Behavior |
| --- | --- |
| `FrontendOnly` | Use only frontend-provided tools |
| `AgentOnly` | Ignore frontend-provided tools |
| `MergeFrontendPriority` | Merge both sides; frontend wins on conflict |
