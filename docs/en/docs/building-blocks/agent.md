---
title: "Agent"
description: "Learn how to define and configure agents in AgentScope .NET 2.0"
---

## Overview

Agent interfaces reside in the `AgentScope.Core.Agent` namespace, and the default implementation is **`EnhancedReActAgent`** (`AgentScope.Core.EnhancedReActAgent`) — a Reasoning-Action (ReAct) loop engine that integrates model, tools, permissions, Hooks, memory, and events into a unified interface.

:::{warning}
The old class `ReActAgent` is fully marked `[Obsolete]`. Use `EnhancedReActAgent` instead. Both share similar Builder methods, but `EnhancedReActAgent` additionally supports Hooks, permission engine, state persistence strategies, HITL confirmation callbacks, and more.
:::

### Core Interfaces

| Interface | Method | Description |
|-----------|--------|-------------|
| `IAgent` / `ICallableAgent` | `CallAsync(IReadOnlyList<Msg>, RuntimeContext?)` → `Task<Msg>` | Runs the reasoning-action loop and returns the final message |
| `IStreamableAgent` | `StreamEventsAsync(Msg, RuntimeContext?)` → `IAsyncEnumerable<Event>` | Same as `CallAsync`, but streams `Event` output (see [Message and Event](./message-and-event.md)) |
| `IAgent` | `ObserveAsync(Msg)` / `ObserveAsync(IReadOnlyList<Msg>)` | Triggers a reply (equivalent to `CallAsync` in `EnhancedReActAgent`) |
| `IAgent` | `Interrupt()` / `Interrupt(Msg)` | Interrupts the currently executing call |
| `IStructuredOutputCapableAgent` | `GenerateStructuredOutputAsync<T>(IEnumerable<Msg>)` → `Task<T>` | Constrains model output to a C# type as JSON and deserializes |
| `IStateModule` | `SaveTo / LoadFrom / LoadIfExists(Session, sessionKey)` | Saves / restores state to/from a `Session` (see [Context and AgentState](./context.md)) |

`HarnessAgent` (`AgentScope.Harness`) implements `IAgent` by internally composing `EnhancedReActAgent` with various Harness subsystems. See [Harness Architecture](../harness/architecture.md).

## Building an Agent

Create an agent via `EnhancedReActAgentBuilder`. **The model is required** — `Build()` throws `InvalidOperationException` if not set.

```csharp
using AgentScope.Core;
using AgentScope.Core.Model;
using AgentScope.Core.Tool;

EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Name("my_agent")                                    // Default "EnhancedReActAgent"
    .SysPrompt("You are a helpful assistant.")            // Default built-in prompt
    .Model(new DashScopeModel("qwen-plus", apiKey))       // Required
    .Memory(new MemoryBase())                             // Optional; default new MemoryBase()
    .MaxIterations(10)                                    // Default 10
    .AddTool(new CalculatorTool())                        // Optional; can be called multiple times
    .Build();
```

### Builder Method Reference

| Method | Parameter | Default | Description |
|--------|-----------|---------|-------------|
| `Name(string)` | Agent name | `"EnhancedReActAgent"` | Used for logging and events |
| `Model(IModel)` | Model instance | **Required** | See [Model](./model.md) |
| `SysPrompt(string)` | System prompt | Built-in default | Runtime read/write via `agent.SystemPrompt` property |
| `Memory(IMemory)` | Memory implementation | `new MemoryBase()` | See [Context and AgentState](./context.md) |
| `AddTool(ITool)` | Single tool | — | Can be called multiple times; see [Tool](./tool.md) |
| `ToolGroupManager(ToolGroupManager)` | Tool group manager | null | Enables tool group activation/deactivation |
| `AddToolGroup(ToolGroup)` | Register a group | — | Auto-creates `ToolGroupManager` |
| `MaxIterations(int)` | Max iterations | `10` | ReAct main loop upper limit |
| `StatePersistence(StatePersistence)` | State persistence strategy | `StatePersistence.All` | Controls whether Memory/Toolkit persists with Session |
| `HookManager(HookManager)` | Hook manager | `new HookManager()` | See below |
| `PermissionEngine(IPermissionEngine)` | Permission engine | null | See [Permission System](./permission-system.md) |
| `Verbose(bool)` | Console verbose logging | `false` | Outputs each iteration step |
| `ConfirmCallback(Func<RequireUserConfirmEvent, Task<ConfirmResult>>)` | HITL confirm callback | null (falls back to console prompt) | Called when a tool is marked Ask by the permission system |
| `AutoApproveOnAsk(bool)` | Auto-approve without terminal | `false` | Takes effect when `Console.IsInputRedirected` and no callback set |
| `Build()` | — | — | Builds the instance |

> The old `ReActAgentBuilder` (`Name/Model/SysPrompt/Memory/AddTool/Tools/ToolGroupManager/AddToolGroup/MaxIterations`) is still provided alongside `ReActAgent` but is deprecated. Method names do not have the `With` prefix, consistent with the new Builder.

## Running an Agent

### CallAsync

```csharp
using AgentScope.Core.Message;

Msg result = await agent.CallAsync(
    Msg.Builder().Role("user").TextContent("What files are in the current directory?").Build());
Console.WriteLine(result.GetTextContent());
```

`HarnessAgent` additionally provides two convenience overloads for single `Msg` and `string` text; `EnhancedReActAgent`'s `CallAsync` accepts `IReadOnlyList<Msg>` and takes the last item as the current turn's user input.

### StreamEventsAsync

```csharp
using AgentScope.Core.Events;

await foreach (Event evt in agent.StreamEventsAsync(userMsg))
{
    if (evt.Type == EventType.ReasoningChunk)
        Console.Write(evt.Message?.GetTextContent());
    if (evt.IsLast) break;
}
```

See [Message and Event](./message-and-event.md) for the complete event model (`Event` + `EventType` enum).

### Structured Output

`GenerateStructuredOutputAsync<T>` injects JSON constraints into the prompt, deserializes the model output into the specified type; throws `ModelException` on parse failure:

```csharp
public record WeatherResponse(string Location, string Temperature, string Condition);

WeatherResponse weather = await agent.GenerateStructuredOutputAsync<WeatherResponse>(
    new[] { Msg.Builder().Role("user").TextContent("How is the weather in San Francisco?").Build() });
Console.WriteLine(weather.Temperature);
```

There is also a streaming version `StreamStructuredOutputAsync<T>(messages, StreamOptions)`, which ultimately delivers a `ReasoningFinish` event carrying JSON text.

## Multi-User / Multi-Session

Each call accepts a `RuntimeContext` (record, `AgentScope.Core.Agent`) carrying `UserId` / `SessionId`:

```csharp
RuntimeContext ctx = RuntimeContext.Empty
    .WithUserId("alice")
    .WithSessionId("session-1");

await agent.CallAsync(msg, ctx);
```

`RuntimeContext` flows via `AsyncLocal` (accessible as `RuntimeContext.Current` throughout the async chain). Middleware and tools can all access the same reference. Session history persistence and restoration depend on the `Memory` configuration. See [Context and AgentState](./context.md).

## Interrupting Execution (Interrupt)

```csharp
agent.Interrupt();                 // Interrupt the current execution
agent.Interrupt(interruptMsg);     // Interrupt with a message
```

Interruption is instance-level: `EnhancedReActAgent` checks a cancellation flag before each iteration. When interrupted, it saves state and returns partial results.

## Hook System

Hooks are called before and after each reasoning / acting / summary phase, decoupled from the agent's main flow:

```csharp
using AgentScope.Core.Hook;

class LoggingHook : HookBase     // HookBase provides virtual default implementations for all
{
    public override Task OnPreReasoningAsync(PreReasoningEvent evt)
    {
        Console.WriteLine($"[{Name}] About to reason, context {evt.Context.Length} characters");
        return Task.CompletedTask;
    }

    public override Task OnPostActingAsync(PostActingEvent evt)
    {
        Console.WriteLine($"[{Name}] Action {evt.Action} success={evt.ActionSuccess}");
        return Task.CompletedTask;
    }
}

var hooks = new HookManager();
hooks.RegisterHook(new LoggingHook());

EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(model)
    .HookManager(hooks)
    .Build();
```

`IHook` provides 10 callback methods + the `Name` property: `OnPreReasoningAsync` / `OnPostReasoningAsync` / `OnPreActingAsync` / `OnPostActingAsync` / `OnPreSummaryAsync` / `OnPostSummaryAsync` / `OnReasoningChunkAsync` / `OnActingChunkAsync` / `OnSummaryChunkAsync` / `OnErrorAsync`. Any Hook that sets `ShouldStop` on the event to `true` will terminate subsequent processing.

## Human-in-the-Loop (HITL)

When a permission engine is configured and a tool call is determined to be `Ask`, `EnhancedReActAgent` will:

1. If `ConfirmCallback` is set, call it and wait for a `ConfirmResult` (`ConfirmResult.Approve()` / `ConfirmResult.Deny(reason)`);
2. Otherwise, fall back to console interaction (`y/N` prompt); when there is no interactive terminal, follow `AutoApproveOnAsk` to either allow or deny.

```csharp
EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(model)
    .PermissionEngine(new PermissionEngine())
    .ConfirmCallback(async confirmEvent =>
    {
        Console.WriteLine($"Tool {confirmEvent.ToolName} requests execution, arguments {confirmEvent.Arguments}");
        return Console.ReadLine() == "y"
            ? ConfirmResult.Approve()
            : ConfirmResult.Deny("User denied");
    })
    .Build();
```

See [Permission System](./permission-system.md) for permission rule configuration.

## State Save and Restore

`EnhancedReActAgent` implements `IStateModule`, saving its state (meta info, memory messages, tool group activation status) into the `Session.Context` dictionary:

```csharp
using AgentScope.Core.Session;

var sessionManager = new SessionManager();
Session session = sessionManager.CreateSession(name: "demo");

// Save after a call
await agent.CallAsync(msg);
agent.SaveTo(session, "main");            // Writes AgentMetaState / Memory / ToolkitState

// After process restart: rebuild agent, restore from the same Session
agent.LoadIfExists(session, "main");      // Silently skips if not found; LoadFrom throws if not found
```

The `StatePersistence` record controls the persistence scope: `StatePersistence.All` (default) / `StatePersistence.None` / `new StatePersistence(MemoryManaged: true, ToolkitManaged: false, PlanNotebookManaged: true)`.

See [Context and AgentState](./context.md) for the complete mechanism and `IAgentStateStore` ecosystem.

## Further Reading

- [Model](./model.md) — Model constructors and streaming interfaces for each provider
- [Tool](./tool.md) — `[Tool]` registration, Toolkit, MCP
- [Permission System](./permission-system.md) — Three-state tool call decisions
- [Harness Architecture](../harness/architecture.md) — Complete `HarnessAgentBuilder` assembly
