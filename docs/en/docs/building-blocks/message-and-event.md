---
title: "Message & Event"
description: "The core data abstractions for agent communication and streaming"
---

Message and event are the two fundamental data structures in AgentScope.

- **Message** — the primitive of agent-to-agent communication and persistence. Each `Msg` is a complete conversation turn, stored in the context and passed between agents.
- **Event** — the primitive of frontend interaction and streaming. Events carry incremental progress updates (text tokens, tool-call fragments, permission requests, …) and drive real-time UIs and human-in-the-loop flows.

The event sequence emitted by a single `CallAsync` always condenses into exactly one assistant `Msg`, ensuring the full message state can be reconstructed from the event stream alone.

## Message

`Msg` (`AgentScope.Core.Message`) represents one turn of conversation — a user input, an agent reply, or a system instruction — with content modelled as an ordered list of typed `ContentBlock`s.

:::{tip}
A single assistant `Msg` corresponds to one full `CallAsync` cycle (multiple reasoning + acting iterations until the final reply).
:::

### Structure

The core fields on `Msg` (via getters):

| Method | Type | Description |
|--------|------|-------------|
| `GetId()` | `string` | Unique message identifier |
| `GetName()` | `string` | Sender name (nullable) |
| `GetRole()` | `MsgRole` | `USER` / `ASSISTANT` / `SYSTEM` / `TOOL` |
| `GetContent()` | `List<ContentBlock>` | Ordered list of content blocks (immutable) |
| `GetMetadata()` | `Dictionary<string, object>` | Arbitrary key/value metadata |
| `GetTimestamp()` | `string` | Creation time (`yyyy-MM-dd HH:mm:ss.SSS`) |
| `GetUsage()` | `ChatUsage` | Token usage (assistant messages only) |
| `GetGenerateReason()` | `GenerateReason` | Termination reason: `MODEL_STOP` / `TOOL_SUSPENDED` / `REASONING_STOP_REQUESTED` / `ACTING_STOP_REQUESTED` / `ALL_TOOLS_DENIED` / `INTERRUPTED` / `MAX_ITERATIONS` |

### Content blocks

Message content is composed of typed blocks, each representing one type of information. Block classes live in `AgentScope.Core.Message`:

| Block | Description | Allowed in |
|-------|-------------|-----------|
| `TextBlock` | Plain text content | USER, ASSISTANT, SYSTEM |
| `DataBlock` | Binary data (image / audio / video) via base64 or URL — unifies the legacy ImageBlock / AudioBlock / VideoBlock | USER, ASSISTANT |
| `ImageBlock` / `AudioBlock` / `VideoBlock` | Legacy concrete media blocks (still supported; new code should prefer `DataBlock`) | USER |
| `ThinkingBlock` | Model reasoning / chain of thought | ASSISTANT |
| `ToolUseBlock` | A tool call: `Id` / `Name` / `Input` / `State` (`ToolCallState`) | ASSISTANT |
| `ToolResultBlock` | A tool result with `State` (`ToolResultState`) | ASSISTANT |
| `HintBlock` | Instructions injected into the loop as user context | ASSISTANT |

:::{note}
Role constraints are enforced at construction: `USER` only allows text/data/image/audio/video blocks; `SYSTEM` only allows `TextBlock`; `ASSISTANT` allows all block types.
:::

### Creating a message

The role-pinned subclasses (`AgentScope.Core.Message.UserMessage` / `AssistantMessage` / `SystemMessage` / `ToolResultMessage`) provide convenient constructors. When `content` is a plain string, it is wrapped in a `TextBlock` automatically.

```csharp
using AgentScope.Core.Message;

// User message — text only
UserMessage userText = new UserMessage("user", "What's in this image?");

// Multi-modal user message
UserMessage userMulti =
        new UserMessage(
                "user",
                TextBlock.Builder().Text("Describe this image:").Build(),
                DataBlock.Builder()
                        .Source(Base64Source.Builder()
                                .Data("...")
                                .MediaType("image/png")
                                .Build())
                        .Build());

// System message — text only
SystemMessage systemMsg = new SystemMessage("system", "You are a helpful assistant.");

// Assistant message — all block types allowed
AssistantMessage assistantMsg = new AssistantMessage("agent", "Here's the result...");
```

For more optional fields (`metadata`, `timestamp`, `usage`, `generateReason`), use each subclass's `Builder()`:

```csharp
UserMessage msg =
        UserMessage.Builder()
                .Name("user")
                .TextContent("Hello")
                .Build();
```

### Accessing content

`Msg` provides helpers for extracting specific block types:

| Method | Returns |
|--------|---------|
| `GetTextContent()` | All `TextBlock`s concatenated by `\n`; empty string when there are none |
| `GetContentBlocks<T>()` | List filtered by type |
| `GetFirstContentBlock<T>()` | The first matching block, or null |
| `HasContentBlocks<T>()` | `true` if a block of the given type exists |

```csharp
using AgentScope.Core.Message;

// All text content
string text = msg.GetTextContent();

// All tool calls
List<ToolUseBlock> toolCalls = msg.GetContentBlocks<ToolUseBlock>();

// Whether there are tool results
if (msg.HasContentBlocks<ToolResultBlock>())
{
    // ...
}
```

## Event

Events are the streaming counterpart of messages. While the agent runs, it emits a sequence of `AgentEvent`s (`AgentScope.Core.Event`) representing incremental progress — text tokens arriving, tool calls being assembled, results streaming back. Each event is lightweight and self-contained.

### Event lifecycle

Every event carries `GetReplyId()`, tying it to the message being assembled. Within a reply, `GetBlockId()` or `GetToolCallId()` acts as a correlation key for events that belong to the same content-block lifecycle. Events follow a **start → delta → end** pattern:

```{mermaid}
sequenceDiagram
    participant Client
    participant Agent

    Agent->>Client: AgentStartEvent

    rect rgba(100, 150, 255, 0.1)
        Note over Client,Agent: Reasoning phase
        Agent->>Client: ModelCallStartEvent
        rect rgba(200, 200, 100, 0.1)
            Note over Client,Agent: TextBlock (blockId)
            Agent->>Client: TextBlockStartEvent
            Agent->>Client: TextBlockDeltaEvent (×N)
            Agent->>Client: TextBlockEndEvent
        end
        rect rgba(200, 200, 100, 0.1)
            Note over Client,Agent: DataBlock (blockId)
            Agent->>Client: DataBlockStartEvent
            Agent->>Client: DataBlockDeltaEvent (×N)
            Agent->>Client: DataBlockEndEvent
        end
        rect rgba(200, 200, 100, 0.1)
            Note over Client,Agent: ToolUseBlock (toolCallId)
            Agent->>Client: ToolCallStartEvent
            Agent->>Client: ToolCallDeltaEvent (×N)
            Agent->>Client: ToolCallEndEvent
        end
        Agent->>Client: ModelCallEndEvent
    end

    rect rgba(100, 255, 150, 0.1)
        Note over Client,Agent: Acting phase
        rect rgba(200, 200, 100, 0.1)
            Note over Client,Agent: ToolResultBlock (toolCallId)
            Agent->>Client: ToolResultStartEvent
            Agent->>Client: ToolResultTextDeltaEvent (×N)
            Agent->>Client: ToolResultDataDeltaEvent (×N)
            Agent->>Client: ToolResultEndEvent
        end
    end

    Agent->>Client: AgentEndEvent
```

All events in one reply share the same `ReplyId`. Within a reply, `BlockId` ties text/thinking/data block events together; `ToolCallId` ties tool calls and tool results. A `BlockId` is scoped to its `ReplyId` and does not have to be a globally unique generated ID. When a block type can have at most one lifecycle within a reply, an implementation may use a stable type key, such as a fixed key for the text block.

### Event types

All events extend `AgentEvent` (`AgentScope.Core.Event`), which exposes the common methods:

| Method | Type | Description |
|--------|------|-------------|
| `GetId()` | `string` | Unique event identifier |
| `GetCreatedAt()` | `string` | ISO 8601 timestamp |
| `GetType()` | `AgentEventType` | Event type enum |
| `GetSource()` | `string` | Source path identifying the originating agent. `null` for top-level agent events; a slash-separated path (e.g. `"main/researcher"`) for events forwarded from a subagent |
| `GetMetadata()` | `Dictionary<string, object>` | Optional key/value bag. Remote subagent forwards also set `TaskId` (`AgentEvent.METADATA_TASK_ID`) to the harness / Agent Protocol task id and `ParentSessionId` (`AgentEvent.METADATA_PARENT_SESSION_ID`) to the parent session when events are task-backed |

Events are grouped below; unless noted otherwise, every event also carries `GetReplyId()` linking it to the message being assembled.

  :::{dropdown} Lifecycle events
**AgentStartEvent** — agent begins a new reply.

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetReplyId()` | `string` | Reply message ID |
    | `GetSessionId()` | `string` | Session ID |
    | `GetName()` | `string` | Agent name |
    | `GetRole()` | `string` | Agent role (default `"assistant"`) |

    **AgentEndEvent** — agent finishes a reply.

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetReplyId()` | `string` | Reply message ID |

    **ExceedMaxItersEvent** — agent hit the max reasoning-acting iteration limit.

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetReplyId()` | `string` | Reply message ID |

    **RequestStopEvent** — early-stop request raised by middleware or a tool.
:::

  :::{dropdown} Text streaming events
**TextBlockStartEvent** — a new text block begins.

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetReplyId()` | `string` | Reply message ID |
    | `GetBlockId()` | `string` | Text-block correlation key within the current reply |

    **TextBlockDeltaEvent** — incremental text content arrives.

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetReplyId()` | `string` | Reply message ID |
    | `GetBlockId()` | `string` | Text-block correlation key within the current reply |
    | `GetDelta()` | `string` | Incremental text content |

    **TextBlockEndEvent** — text block completes.

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetReplyId()` | `string` | Reply message ID |
    | `GetBlockId()` | `string` | Text-block correlation key within the current reply |
:::

  :::{dropdown} Thinking streaming events
**ThinkingBlockStartEvent / ThinkingBlockDeltaEvent / ThinkingBlockEndEvent** — same shape as the text streaming events; specific to the model's chain of thought. Its `BlockId` has the same reply-scoped correlation-key semantics.
:::

  :::{dropdown} Data streaming events
**DataBlockStartEvent / DataBlockDeltaEvent / DataBlockEndEvent** — same shape as the text streaming events, carrying images / audio / video binary data:

    - `DataBlockStartEvent`: `GetMediaType()` returns the MIME type (e.g. `"image/png"`).
    - `DataBlockDeltaEvent`: `GetData()` returns incremental base64-encoded data.
:::

  :::{dropdown} Tool-call streaming events
**ToolCallStartEvent** — agent begins a tool call.

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetReplyId()` | `string` | Reply message ID |
    | `GetToolCallId()` | `string` | Unique tool call ID |
    | `GetToolCallName()` | `string` | The tool being called |

    **ToolCallDeltaEvent** — incremental tool-call arguments arrive; `GetDelta()` returns a JSON fragment.

    **ToolCallEndEvent** — tool-call arguments complete.
:::

  :::{dropdown} Tool-result streaming events
**ToolResultStartEvent** — tool starts executing (carries `ToolCallId`, `ToolCallName`).

    **ToolResultTextDeltaEvent** — incremental text output from the tool; `GetDelta()` returns a text fragment.

    **ToolResultDataDeltaEvent** — incremental binary output from the tool; similar to `DataBlockDeltaEvent` with `MediaType` / `Data` / `Url`.

    **ToolResultEndEvent** — tool completes.

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetReplyId()` | `string` | Reply message ID |
    | `GetToolCallId()` | `string` | The matching tool call ID |
    | `GetState()` | `ToolResultState` | Final state: `SUCCESS`, `ERROR`, `INTERRUPTED`, `DENIED`, `RUNNING` |
:::

  :::{dropdown} Model-call events
**ModelCallStartEvent** — model API call starts (carries `ModelName`).

    **ModelCallEndEvent** — model API call completes (carries `InputTokens` / `OutputTokens`).
:::

  :::{dropdown} Human-in-the-loop events
**RequireUserConfirmEvent** — agent pauses for user confirmation.

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetReplyId()` | `string` | Reply message ID |
    | `GetToolCalls()` | `List<ToolUseBlock>` | Tool calls awaiting confirmation |

    **RequireExternalExecutionEvent** — agent pauses for external execution.

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetReplyId()` | `string` | Reply message ID |
    | `GetToolCalls()` | `List<ToolUseBlock>` | Tool calls awaiting external execution |

    **UserConfirmResultEvent** — emitted when a later `CallAsync()` resumes a paused permission HITL request.
    It carries one or more `ConfirmResult`s, and its `ReplyId` matches the earlier `RequireUserConfirmEvent`.

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetReplyId()` | `string` | Reply ID of the correlated `RequireUserConfirmEvent` |
    | `GetConfirmResults()` | `List<ConfirmResult>` | Confirmation results accepted for this resume |

    **ExternalExecutionResultEvent** — emitted when a later `CallAsync()` resumes a paused external-execution request.
    It carries one or more `ToolResultBlock`s, and its `ReplyId` matches the earlier `RequireExternalExecutionEvent`.

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetReplyId()` | `string` | Reply ID of the correlated `RequireExternalExecutionEvent` |
    | `GetToolResults()` | `List<ToolResultBlock>` | External execution results accepted for this resume |

    **AllToolsDeniedEvent** — the user denied all tool calls from the most recent reasoning step via HITL confirmation. This event is emitted through the `OnActing` middleware chain, allowing middlewares to emit a `RequestStopEvent` to stop the agent. If no middleware handles it, the agent continues to the next reasoning iteration (backward compatible).

    | Method | Type | Description |
    |--------|------|-------------|
    | `GetDeniedToolCalls()` | `List<ToolUseBlock>` | The denied tool calls |
:::

  :::{dropdown} Subagent events
**SubagentExposedEvent** — a subagent spawned via `agent_spawn(expose_to_user=true)` has been exposed as a user-addressable entry point. SSE / streaming consumers can use this to render a new conversation entry in the UI.

| Method | Type | Description |
|--------|------|-------------|
| `GetSubagentId()` | `string` | Unique identifier of the subagent |
| `GetAgentId()` | `string` | Agent type ID of the subagent |
| `GetSessionId()` | `string` | Session ID of the subagent |
| `GetLabel()` | `string` | User-visible label (optional) |
:::

## Reconstructing messages from events

Events and messages are not separate worlds — they are two views of the same data. The event stream from `StreamEvents` can be aggregated by `ReplyId` / `BlockId` / `ToolCallId` to reconstruct a complete `AssistantMessage`, ensuring the final message state is fully recoverable from events alone.

See `AgentScope.Core`'s `Agent/StreamingHook.cs` and `agentscope-examples/documentation/.../streaming/AgentEventStreamExample.cs` for the standard pattern of grouping by block ID and accumulating content.

```csharp
using AgentScope.Core.Event;
using AgentScope.Core.Message;
using System.Text;

StringBuilder accumulated = new StringBuilder();

await foreach (var evt in agent.StreamEvents(userMsg))
{
    if (evt is AgentStartEvent start)
    {
        Console.WriteLine("[start replyId=" + start.GetReplyId() + "]");
    }
    else if (evt is TextBlockDeltaEvent delta)
    {
        accumulated.Append(delta.GetDelta());
    }
    else if (evt is ToolCallStartEvent tc)
    {
        Console.WriteLine("[tool] " + tc.GetToolCallName());
    }
    else if (evt is ToolResultEndEvent end)
    {
        Console.WriteLine("[tool result state=" + end.GetState() + "]");
    }
    else if (evt is AgentEndEvent end)
    {
        Console.WriteLine("\n[end] full text:\n" + accumulated);
    }
}
```

:::{tip}
This decoupling makes deployments flexible: the backend pushes the event stream over SSE, and the frontend reconstructs the message client-side. Even if the connection drops, replaying events from any checkpoint restores the message state precisely.
:::

### Example: streaming UI

A typical streaming UI loop (an ASP.NET Core SSE form is shown in `streaming/StreamingWebExample.cs`):

```csharp
using AgentScope.Core.Event;
using AgentScope.Core.Message;

await foreach (var evt in agent.StreamEvents(new UserMessage("user", "Help me fix this bug")))
{
    if (evt is AgentStartEvent start)
    {
        Console.WriteLine("[start replyId=" + start.GetReplyId() + "]");
    }
    else if (evt is TextBlockDeltaEvent delta)
    {
        Console.Write(delta.GetDelta());
    }
    else if (evt is ToolCallStartEvent tc)
    {
        Console.WriteLine("\n[calling " + tc.GetToolCallName() + "...]");
    }
    else if (evt is ToolResultEndEvent end)
    {
        Console.WriteLine("[tool finished: " + end.GetState() + "]");
    }
    else if (evt is AgentEndEvent end)
    {
        Console.WriteLine("\n[done]");
    }
}
```

## Further reading

::::{grid} 2

:::{grid-item-card} Agent
:link: ./agent.html

How agents emit events and messages in the ReAct loop
:::
  :::{grid-item-card} Context
:link: context.html

How messages are stored and persisted
:::

::::
