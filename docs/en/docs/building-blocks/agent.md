---
title: "Agent"
description: "Learn how to define and configure agents in AgentScope .NET"
---

## Overview

`IAgent` (interface at `AgentScope.Core.Agent.IAgent`, default implementation `ReActAgent`) is the core abstraction — a reasoning-acting loop engine that integrates models, tools, the permission system, human-in-the-loop, context management, middlewares, state management, and the event system into a single unified interface.

Its primary responsibilities are:

- Receive input messages or events; orchestrate tools to complete tasks.
- Manage context (conversation history is held on `AgentState.Context` and can be persisted automatically via an `IAgentStateStore`).
- Provide middleware hooks at key lifecycle points for custom logic.
- Manage concurrent and sequential tool execution automatically.

### Core interface

The `IAgent` interface composes three capability interfaces: `ICallableAgent`, `IStreamableAgent`, `IObservableAgent`. The most commonly used methods:

| Method | Description |
|--------|-------------|
| `CallAsync(List<Msg>)` / `CallAsync(List<Msg>, RuntimeContext)` | Run the reasoning-acting loop and return `Task<Msg>` |
| `StreamEvents(List<Msg>)` / `StreamEvents(Msg)` | Same loop, but emits `AgentEvent`s incrementally |
| `ObserveAsync(Msg)` / `ObserveAsync(List<Msg>)` | Append messages to context without triggering reasoning (returns `Task`) |

`ReActAgent` adds overloads for structured output (`CallAsync(msgs, structuredOutputType, runtimeContext)`) and convenient per-call metadata via `RuntimeContext`.

### Main loop

Each `CallAsync` runs through the reasoning-acting loop. The diagram below shows the main control flow:

```{mermaid}
flowchart TD
    A([Input: messages / event]) --> B{Waiting on\nexternal event?}
    B -- yes --> C[Apply event\nupdate tool state]
    B -- no --> D[Append to context]
    C --> E
    D --> E

    E{Decide next action} -- exit --> F([Return: waiting on\nexternal interaction])
    E -- reason --> G[Compress context if needed]
    G --> H[LLM call]
    H -- no tool calls --> I([Return final message])
    H -- tool calls --> Acting

    subgraph Acting [Acting]
        direction TB
        J[Batch tool calls\nserial / concurrent] --> L[Execute tool calls]
        L --> M{Permission\ncheck}
        M -- allow --> N[Run tool → result]
        M -- ask / external --> O([Pause and emit\nRequireUserConfirmEvent])
        M -- deny --> P[Return error to LLM]
    end

    N --> E
    P --> E
```

## Configuring an agent

Build an agent with `ReActAgent.Builder()`...`.Build()`. `.Model(...)` takes either a `ModelRegistry`-resolved string id (most common — picks up env vars automatically) or an explicit `IModel` instance (when you need explicit control over timeouts / custom endpoints / etc.).

::::{tab-set}
:::{tab-item} String model id (recommended)
```csharp
using AgentScope.Core;
using AgentScope.Core.Tool;

ReActAgent agent =
        ReActAgent.Builder()
                .Name("my_agent")
                .SysPrompt("You are a helpful assistant.")
                // Resolved by ModelRegistry; reads DASHSCOPE_API_KEY automatically.
                // Switch providers by using "openai:gpt-5.5" / "anthropic:claude-sonnet-4-5"
                // / "deepseek:deepseek-v4-flash" / "gemini:gemini-2.0-flash" / "ollama:llama3".
                .Model("dashscope:qwen-plus")
                .Toolkit(new Toolkit())
                .Build();
```
:::
:::{tab-item} Explicit Model builder
```csharp
using AgentScope.Core;
using AgentScope.Extensions.Model.DashScope;
using AgentScope.Extensions.Model.DashScope.Formatter;
using AgentScope.Core.Tool;

ReActAgent agent =
        ReActAgent.Builder()
                .Name("my_agent")
                .SysPrompt("You are a helpful assistant.")
                .Model(
                        DashScopeChatModel.Builder()
                                .ApiKey("YOUR_API_KEY")
                                .ModelName("qwen-max")
                                .Stream(true)
                                .Formatter(new DashScopeChatFormatter())
                                .Build())
                .Toolkit(new Toolkit())
                .Build();
```
:::
:::{tab-item} With Toolkit / MCP
```csharp
using AgentScope.Core;
using AgentScope.Core.Tool;
using AgentScope.Core.Tool.Builtin;
using AgentScope.Core.Tool.Mcp;

Toolkit toolkit = new Toolkit();
toolkit.RegisterTool(new TodoTools());          // reflectively register [Tool] methods
toolkit.RegisterTool(new MyCustomTools());      // custom tool class

McpClientWrapper amap = McpClientBuilder.StreamableHttp()
        .Name("amap")
        .Url("https://mcp.amap.com/mcp?key=" + Environment.GetEnvironmentVariable("AMAP_API_KEY"))
        .Build();
await toolkit.RegisterMcpClientAsync(amap);

ReActAgent agent =
        ReActAgent.Builder()
                .Name("my_agent")
                .SysPrompt("You are a helpful assistant.")
                .Model("dashscope:qwen-max")
                .Toolkit(toolkit)
                .Build();
```
:::
::::

:::{tip}
The `ModelRegistry` string form (`<provider>:<model>`) requires the matching model extension NuGet package. It supports `dashscope` / `openai` / `deepseek` / `anthropic` / `gemini` / `ollama` and reads the matching API key (`DASHSCOPE_API_KEY` / `OPENAI_API_KEY` / `DEEPSEEK_API_KEY` / `ANTHROPIC_API_KEY` / `GEMINI_API_KEY`) from the environment. For long-running scenarios that also need a workspace, session persistence, memory compaction, subagents, and so on, use [`HarnessAgent`](../harness/architecture.md) — it is a thin wrapper around `ReActAgent` with a largely identical builder.
:::

### Builder fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Name` | `string` | required | Agent identifier, used for messages and logs |
| `SysPrompt` | `string` | required | The base system prompt |
| `Model` | `IModel` | required | The LLM driving reasoning (extends `ChatModelBase`) |
| `Toolkit` | `Toolkit` | `new Toolkit()` | Manages tools, MCP clients, skills, and tool groups |
| `Middlewares` | `List<MiddlewareBase>` | `new List<MiddlewareBase>()` | Applied to agent / reasoning / acting / model call / system prompt hooks |
| `StateStore` | `IAgentStateStore` | `null` (no persistence) | When set, agent automatically loads/saves `AgentState` on every call, keyed by the `(UserId, SessionId)` of the call's `RuntimeContext` |
| `DefaultSessionId` | `string` | agent `Name` | Fallback `SessionId` used when a call's `RuntimeContext` carries none |
| `PermissionContext` | `PermissionContextState` | `DEFAULT` mode | Fine-grained tool execution rules, see [Permission System](./permission-system.md) |
| `ModelConfig` | `ModelConfig` | default | Model retries and fallback model |
| `ReactConfig` | `ReactConfig` | default | Max iterations and reject handling |
| `MaxIters` | `int` | `10` | Max iterations of the ReAct main loop (alternative to `ReactConfig`) |

## Multi-user / multi-session concurrency

`ReActAgent` is **stateless between calls** — a single instance can serve multiple users and sessions concurrently. Each `CallAsync()` uses the `(UserId, SessionId)` carried by its `RuntimeContext` to locate the correct conversation state; different sessions are fully isolated.

```csharp
using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.State;

// Create one agent instance at application startup (singleton)
ReActAgent agent = ReActAgent.Builder()
        .Name("assistant")
        .SysPrompt("You are a helpful assistant.")
        .Model("dashscope:qwen-plus")
        .StateStore(new JsonFileAgentStateStore(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".agentscope/sessions")))
        .Build();

// In your HTTP handler — different requests pass different RuntimeContexts, fully isolated
await agent.CallAsync(new List<Msg> { new UserMessage("Hello") },
        RuntimeContext.Builder().UserId("alice").SessionId("session-1").Build());

await agent.CallAsync(new List<Msg> { new UserMessage("Hi there") },
        RuntimeContext.Builder().UserId("bob").SessionId("session-2").Build());
```

At the start of each `CallAsync()`, the agent automatically loads the `AgentState` (conversation context, permission rules, etc.) for the given `(UserId, SessionId)`. When the call finishes, the state is saved back. Different sessions are completely isolated.

:::{tip}
Calls targeting the same `(UserId, SessionId)` are **serialized** — a second request waits for the first to complete. Calls targeting different sessions run in parallel.
:::

A complete ASP.NET Core example: `agentscope-examples/documentation/.../streaming/StreamingWebExample.cs`.

## Interrupt

To cancel an in-flight call from the outside (user cancellation, timeout, graceful shutdown), use `Interrupt`:

```csharp
using AgentScope.Core.Agent;

// Identify the target session
RuntimeContext target = RuntimeContext.Builder()
        .UserId("alice")
        .SessionId("session-001")
        .Build();

// Interrupt the in-flight call for that session
agent.Interrupt(target);

// Interrupt with a message — the LLM sees this message when the session resumes
agent.Interrupt(target, new UserMessage("User cancelled the operation"));
```

Interrupt is **per-session**: it only affects the call running on the specified `(UserId, SessionId)` — other concurrent sessions on the same agent are unaffected.

**What happens after interrupt:**
- The current reasoning/tool execution is stopped at the next checkpoint (start of reasoning, start of acting, each streaming chunk)
- The agent returns a Msg tagged with `GenerateReason.INTERRUPTED`
- The conversation state (AgentState) is saved automatically — the next `CallAsync()` to the same session resumes from the interruption point

You can also use raw `(UserId, SessionId)` strings:

```csharp
agent.Interrupt("alice", "session-001");
agent.Interrupt("alice", "session-001", interruptMsg);
```

## Running an agent

`CallAsync` and `StreamEvents` accept the same input messages and drive the same reasoning-acting loop. They differ in how the result is delivered.

### CallAsync

`CallAsync` consumes all events internally and returns the final `Msg` when the agent finishes or pauses for external interaction.

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

UserMessage msg = new UserMessage("What files are in the current directory?");
Msg result = await agent.CallAsync(new List<Msg> { msg }, RuntimeContext.Empty);
Console.WriteLine(result.GetTextContent());
```

### StreamEvents

`StreamEvents` emits `AgentEvent`s one by one so you can stream text, tool-call progress, and lifecycle events to your UI in real time. Dispatch on `event.GetType()` to handle each kind:

```csharp
using AgentScope.Core.Event;
using AgentScope.Core.Message;

await foreach (var evt in agent.StreamEvents(new UserMessage("Summarize the README.")))
{
    if (evt is TextBlockDeltaEvent delta)
    {
        // Streaming text fragment — append to UI or stdout
        Console.Write(delta.GetDelta());
    }
    else if (evt is ToolCallStartEvent tc)
    {
        // The agent is about to call a tool — surface the call info
        Console.WriteLine("\n[tool] " + tc.GetToolCallName());
    }
    // Other events: thinking blocks, tool results, reply end, etc.
}
```

Full event-type and field reference: [Message and event](./message-and-event.md).

### ObserveAsync

Use `ObserveAsync` to inject a message into the agent's context without triggering a reply — useful in multi-agent setups where one agent observes another agent's output.

```csharp
await agent.ObserveAsync(otherAgentMsg);
```

## RuntimeContext (per-call context)

`RuntimeContext` (`AgentScope.Core.Agent.RuntimeContext`) is a **per-call metadata bag**: pass one instance to `CallAsync` / `Stream`, and the agent binds it for the duration of that call so downstream tools, middlewares, and hooks all observe the same reference. The framework unbinds it on completion.

It is **not** persistent state — `AgentState` (conversation context, compressed summaries, permission rules, tool state) covers that. `RuntimeContext` carries data that is scoped to a single invocation: tenant / userId / request-id, DB connections, audit loggers, feature flags, and so on.

### Built-in fields and attribute layers

`RuntimeContext` exposes three kinds of slot:

| Slot | Set via | Read via |
|------|---------|----------|
| Session fields | `SessionId(string)` / `UserId(string)` | `GetSessionId()` / `GetUserId()` |
| String attributes (free-form key-value) | `Put(string key, object value)` | `T Get<T>(string key)` |
| Typed attributes (inject business POJOs by `Type`) | `Put<T>(T value)` / `Put<T>(string key, T value)` | `T Get<T>()` / `T Get<T>(string key)` |

Typed attributes power tool injection — declare a parameter of the matching type on a `[Tool]` method and the framework supplies the value. See [Tool — Receiving context](./tool.md#receiving-context). String attributes are typically used for in-process coordination (e.g. middleware-to-middleware signalling). The two layers are isolated: typed values do not appear in `GetExtra()` and vice-versa.

### Construct and pass

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

RuntimeContext ctx =
        RuntimeContext.Builder()
                .UserId("alice")                                             // optional; null = anonymous
                .SessionId("session-001")                                    // selects the state slot
                .Put("request_id", "req-abc-123")                            // string layer
                .Put<UserContext>(new UserContext("alice", "en"))            // typed layer (POJO)
                .Build();

Msg result = await agent.CallAsync(new List<Msg> { new UserMessage("Hi.") }, ctx);
```

`ReActAgent` provides `RuntimeContext` overloads for `CallAsync` and `Stream`; `StreamEvents` does not — when you need a context with the event stream, use `Stream(msgs, options, ctx)`, or configure a global `ToolExecutionContext` on the builder. When no context is passed the framework substitutes `RuntimeContext.Empty` (null session fields, empty attribute maps), and the agent falls back to its builder-time `DefaultSessionId`.

### Who reads it

- **Tools** (`[Tool]` methods and `ToolBase.CallAsync`) — see [Tool — Receiving context](./tool.md#receiving-context).
- **Middleware** (every `MiddlewareBase` hook) — received as the second parameter `ctx`. See [Middleware — Reading RuntimeContext](./middleware.md#reading-runtimecontext).
- **All threads within the same call** — the internal maps are concurrent, so hooks and tools can read/write the same instance to coordinate.

### Relation to persistence

- Free-form / typed `RuntimeContext` attributes never enter `AgentState` and are never written back by the `IAgentStateStore`.
- The `SessionId` / `UserId` fields **do** drive persistence: each call activates the `(UserId, SessionId)` state slot, so passing different identities on `RuntimeContext` retargets which `AgentState` is loaded and saved. When absent, the agent falls back to its builder-time `DefaultSessionId`.

Runnable examples: `agentscope-examples/documentation/.../context/RuntimeContextExample.cs`, `tool/ToolExecutionContextExample.cs`.

:::{note}
A legacy `ToolExecutionContext` (`AgentScope.Core.Tool`) is `[Obsolete]`. New code should use `RuntimeContext`. The legacy type is bridged automatically via `RuntimeContext.AsToolExecutionContext()`, so existing code keeps working.
:::

## Human-in-the-loop

The agent pauses and emits a special event in two cases: a tool call requiring **user confirmation** (the permission system returned ASK), or a tool marked as **external execution** (the result must come from outside the agent). In both cases, you resume the agent by feeding the result back through the next `CallAsync`.

### User confirmation

When the permission system decides a tool call needs user approval, the agent emits `RequireUserConfirmEvent` and pauses.

**1. Receive `RequireUserConfirmEvent`** — use `StreamEvents` to detect the pause. The event carries `GetReplyId()` (used to resume) and `GetToolCalls()` — a list of `ToolUseBlock` each exposing `GetId()` / `GetName()` / `GetInput()` / `GetSuggestedRules()`.

```csharp
using AgentScope.Core.Event;

await foreach (var evt in agent.StreamEvents(msg))
{
    if (evt is RequireUserConfirmEvent confirm)
    {
        foreach (var tc in confirm.GetToolCalls())
        {
            Console.WriteLine("Tool: " + tc.GetName() + ", input: " + tc.GetInput());
            Console.WriteLine("Suggested rules: " + tc.GetSuggestedRules());
        }
    }
}
```

**2. Build confirm results** — construct a `ConfirmResult` per pending call. You can tweak the tool input on the way back, or accept the suggested rules so identical future calls auto-allow:

```csharp
using AgentScope.Core.Event;
using System.Collections.Generic;

List<ConfirmResult> confirmResults = new List<ConfirmResult>();
foreach (var tc in confirmEvent.GetToolCalls())
{
    confirmResults.Add(
            new ConfirmResult(
                    /* confirmed = */ true,                  // false to deny
                    /* toolCall  = */ tc,                    // pass back (optionally modified)
                    /* rules     = */ tc.GetSuggestedRules() // accept rules → future calls auto-allow
                    ));
}
```

**3. Resume the agent** — pass `confirmResults` to the next `CallAsync` via metadata:

```csharp
using AgentScope.Core.Message;

UserMessage resumeMsg =
        UserMessage.Builder()
                .Metadata(new Dictionary<string, object> {
                        { Msg.METADATA_CONFIRM_RESULTS, confirmResults }
                })
                .Build();

Msg result = await agent.CallAsync(new List<Msg> { resumeMsg }, RuntimeContext.Empty);
```

- **Confirmed** tool calls execute immediately; the agent continues reasoning.
- **Denied** tool calls produce an error result visible to the LLM, which may try a different approach.
- **Accepted rules** are persisted in the permission engine — matching future calls will be auto-allowed without prompting.

### External tool execution

When the agent invokes a tool with `IsExternalTool() == true`, it emits `RequireExternalExecutionEvent` and pauses. The tool's logic runs outside the agent — typically by a human operator or external system.

**1. Receive `RequireExternalExecutionEvent`** — same shape as user confirmation: `GetReplyId()` plus a list of `GetToolCalls()` awaiting external execution.

```csharp
using AgentScope.Core.Event;

await foreach (var evt in agent.StreamEvents(msg))
{
    if (evt is RequireExternalExecutionEvent ext)
    {
        foreach (var tc in ext.GetToolCalls())
            Console.WriteLine("External execution: " + tc.GetName() + "(" + tc.GetInput() + ")");
    }
}
```

**2. Execute externally and build results** — run the action outside the agent and wrap each result as a `ToolResultBlock`:

```csharp
using AgentScope.Core.Message;
using System.Collections.Generic;

List<ToolResultBlock> executionResults = new List<ToolResultBlock>();
foreach (var tc in externalEvent.GetToolCalls())
{
    string output = RunExternalOperation(tc.GetName(), tc.GetInput());
    executionResults.Add(
            ToolResultBlock.Builder()
                    .Id(tc.GetId())
                    .Name(tc.GetName())
                    .Output(new List<ContentBlock> { TextBlock.Builder().Text(output).Build() })
                    .State(ToolResultState.SUCCESS)
                    .Build());
}
```

**3. Resume the agent** — feed the results back as the next `CallAsync`'s input message. After the results are validated, they are injected into the agent context and the agent emits `ExternalExecutionResultEvent`; its `GetReplyId()` matches the earlier `RequireExternalExecutionEvent.GetReplyId()`. Reasoning then continues from where it paused.

:::{tip}
Use `StreamEvents` when building interactive UIs — it lets you detect pauses in real time and prompt the user immediately. Use `CallAsync` for programmatic flows that handle events automatically. Complete runnable examples: `agentscope-examples/documentation/.../hitl/PermissionHITLExample.cs`.
:::

## Configuring state persistence (IAgentStateStore)

`AgentState` holds everything required to resume the agent — conversation context, compressed summaries, permission rules, tool state, and the current reply position. [`IAgentStateStore`](../../integration/session/index.md) is its storage abstraction.

**Set `StateStore(...)` on the builder and the agent persists and recovers automatically**: every `CallAsync` writes `AgentState` back; the next time you call with the same `(UserId, SessionId)`, it loads. The agent instance is stateless with respect to sessions — the slot is chosen per-call from the `RuntimeContext` (falling back to `DefaultSessionId`).

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.State;

ReActAgent agent = ReActAgent.Builder()
        .Name("my_agent")
        .SysPrompt("You are a helpful assistant.")
        .Model(model)
        .Toolkit(new Toolkit())
        .StateStore(new JsonFileAgentStateStore(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".agentscope/sessions")))
        .Build();

// Pick the slot for this conversation. userId is optional (null = anonymous).
RuntimeContext rc = RuntimeContext.Builder()
        .UserId("user_123")
        .SessionId("session_789")
        .Build();

// Auto-loaded if data exists for (user_123, session_789); auto-persisted when the call completes.
await agent.CallAsync(new List<Msg> { new UserMessage("Resume the previous task.") }, rc);
```

Built-in and extension implementations:

| Implementation | Module | When to use |
|----------------|--------|-------------|
| `InMemoryAgentStateStore` | `AgentScope.Core` | unit tests / single-process demos |
| `JsonFileAgentStateStore` | `AgentScope.Core` | single-machine dev; JSON per `(UserId, SessionId)` directory |
| `RedisAgentStateStore` | `AgentScope.Extensions.Redis` | multi-replica production; shared across processes and nodes |
| `MysqlAgentStateStore` | `AgentScope.Extensions.MySql` | when state must live in a relational store (audit / reporting) |

A single `SessionId` is enough for most cases. For per-user partitioning, also set `UserId` on the `RuntimeContext`; the store addresses each slot by the `(UserId, SessionId)` pair.

Use `agent.GetAgentState(userId, sessionId)` or `agent.GetAgentState(runtimeContext)` to inspect a specific session's state:

```csharp
AgentState state = agent.GetAgentState("alice", "session-001");
state.GetContext().Count;                  // current message count
string json = state.ToJson();               // serialize to JSON
```

For full field-by-field details, cross-node continuation, and how the state store interacts with compaction / Plan Mode / subagents, see [Context & AgentState](context.md) and [Compaction](../harness/compaction.md).

## Structured Output

Structured output forces the agent to respond according to a JSON Schema you specify, rather than free-form text. Use it whenever your code needs to consume the agent's output programmatically — form filling, data extraction, classification, etc.

### Basic usage

Pass a C# class (or `JsonNode` schema) to `CallAsync`:

```csharp
using AgentScope.Core.Message;

// Define the output structure
public record WeatherResponse(string Location, string Temperature, string Condition);

Msg result = await agent.CallAsync(
        new List<Msg> { new UserMessage("What's the weather in SF?") },
        typeof(WeatherResponse));

// Extract strongly-typed data from the result
WeatherResponse weather = result.GetStructuredData<WeatherResponse>();
Console.WriteLine(weather.Location);      // "San Francisco"
Console.WriteLine(weather.Temperature);   // "18°C"
```

Structured output works alongside tools — the agent can call tools to gather information first, then emit the final result in the specified schema.

### How it works

The framework automatically selects the implementation path based on model capabilities:

| Path | Condition | Behavior |
|------|-----------|----------|
| **Native** | Model supports `response_format` with tools (OpenAI, DashScope, etc.) | JSON Schema is passed directly to the model API via `response_format`; the model guarantees valid JSON output, and the loop terminates naturally |
| **Fallback** | Model lacks native structured output (Anthropic, Ollama, etc.) | A synthetic `generate_response` tool is injected with an instruction hint; the model calls this tool to emit its structured result |

Either way, the caller's code is identical — path selection is transparent.

```
┌─── CallAsync(msgs, Schema.class) ───┐
│                                     │
│   model.SupportsNative...?          │
│      ├─ yes → response_format       │  ← zero overhead, model-native
│      └─ no  → generate_response     │  ← synthetic tool + instruction
│                                     │
└──── returns Msg with schema ────────┘
```

### Reading the result

The `Msg` returned by `CallAsync` carries the parsed structured data in its metadata:

```csharp
// Option 1: strongly-typed extraction
WeatherResponse data = result.GetStructuredData<WeatherResponse>();

// Option 2: read as Dictionary
var map = (Dictionary<string, object>)result.GetMetadata()["_structured_output"];
```

### Using a JsonNode schema

If you prefer not to define a class, pass a raw JSON Schema:

```csharp
using System.Text.Json;
using System.Text.Json.Nodes;

JsonNode schema = JsonNode.Parse("""
    {
      "type": "object",
      "properties": {
        "sentiment": { "type": "string", "enum": ["positive", "negative", "neutral"] },
        "confidence": { "type": "number" }
      },
      "required": ["sentiment", "confidence"]
    }
    """);

Msg result = await agent.CallAsync(
        new List<Msg> { new UserMessage("Analyze the sentiment of this review") },
        schema);
```

## More capabilities

The following features are configured via the builder. See their respective documentation for details:

### Model fault tolerance

```csharp
ReActAgent.Builder()
        .Model("dashscope:qwen-plus")
        .MaxRetries(3)                              // auto-retry on model call failure
        .FallbackModel("dashscope:qwen-max")        // switch to fallback after consecutive failures
        .Build();
```

### Skills

Skills are hot-loadable Markdown prompt modules that the LLM activates on demand:

```csharp
ReActAgent.Builder()
        .SkillRepository(new MysqlSkillRepository(dataSource))
        .Build();
```

### Built-in tools

| Builder method | Description |
|---|---|
| `EnableMetaTool(true)` | Registers `list_tools` / `activate_group` meta tools — lets the LLM discover and switch tool groups |
| `EnableTaskList()` | Registers task-list tools — lets the LLM decompose complex tasks into steps and track progress |

## Further reading

::::{grid} 2

:::{grid-item-card} Permission System
:link: ./permission-system.html

Control which tools the agent can call, and under what conditions.
:::

:::{grid-item-card} Middleware
:link: ./middleware.html

Intercept and modify agent behavior at the agent, reasoning, acting, and model-call hooks.
:::

::::
