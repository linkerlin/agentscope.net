---
title: "FAQ"
description: "FAQ: build, model, state, streaming events"
---

## Build and Dependencies

**Q: Do I need to install model extension packages?**

No. OpenAI / DashScope / Anthropic / Gemini / DeepSeek / Ollama / Mock are all built into `AgentScope.Core`.

**Q: What is the project target framework?**

`net10.0` (source `TargetFramework` is net10.0, please use .NET 10 SDK or later to build).

**Q: How to choose between `AgentScope.Harness` and `AgentScope.Core`?**

Use `AgentScope.Harness` when you need workspace / middleware pipeline / subagent / transcript and other engineering capabilities; use `AgentScope.Core` when you only need a bare reasoning loop. `AgentScope.Harness` references `AgentScope.Core`.

## Model

**Q: Why does constructing `DashScopeModel` throw an error?**

Check the parameter order: `DashScopeModel(string modelName, string? apiKey = null, ...)`. Do not write `new DashScopeModel(apiKey, "qwen-plus")`.

**Q: How to run through the flow without an API Key?**

`MockModel.Builder().ModelName("mock-model").Build()`, echoes the last message, does not make network requests.

**Q: Does Gemini support streaming?**

`GeminiModel` does not implement `IStreamingChatModel`; `EnhancedReActAgent` returns text in full chunks. For streaming, use OpenAI / DashScope / Anthropic / DeepSeek / Ollama.

## Message and Events

**Q: `new UserMessage("text")` fails to compile?**

`UserMessage` has no single-arg text constructor. Use `Msg.Builder().Role("user").TextContent("text").Build()`, or `new UserMessage(null, "text")`.

**Q: What does `StreamEventsAsync` return? How to iterate?**

`IAsyncEnumerable<Event>`, use `await foreach`:

```csharp
await foreach (Event evt in agent.StreamEventsAsync(msg))
{
    if (evt.Type == EventType.ReasoningChunk) Console.Write(evt.Message?.GetTextContent());
    if (evt.IsLast) break;
}
```

It is not `IObservable`, cannot use `Subscribe`.

**Q: What is the relationship between `AgentEvent` and `Event`?**

`Event` (with `EventType` enum) is the actual streaming event produced by the ReAct loop; `AgentEvent` is a fine-grained record hierarchy (`TextBlockDeltaEvent`, etc.) used by the protocol adaptation layer (A2A / AgUI).

## Agent

**Q: Can `ReActAgent` still be used?**

It can be used but is marked `[Obsolete]`; please migrate to `EnhancedReActAgent`.

**Q: How to make the agent remember the previous turn?**

`EnhancedReActAgent` internally uses `Memory` to maintain context: default `MemoryBase` retains within the instance; for cross-restart use `SqliteMemory(path)` or `StateBackedMemory(store, initial, key)`; for session-level save/restore use `agent.SaveTo(session, key)` / `agent.LoadIfExists(session, key)`.

**Q: Is it safe for multiple users to share one agent instance?**

Yes. The agent is a stateless engine; each call carries `(UserId, SessionId)` via `RuntimeContext`; calls from different sessions do not interfere (memory implementation must provide isolation, e.g., selecting store by session).

**Q: How to interrupt a running call?**

`agent.Interrupt()` (or `Interrupt(Msg)`), `EnhancedReActAgent` responds at iteration checkpoints.

**Q: How to configure HITL confirmation?**

Builder: `PermissionEngine(...)` + `ConfirmCallback(...)` (or `AutoApproveOnAsk(true)`). Tool calls with a permission verdict of `Ask` trigger the callback.

## Harness

**Q: Why doesn't `HarnessAgentBuilder` have `WithModel("dashscope:qwen-plus")`?**

2.0 removed string model IDs; models must be passed as `IModel` instances: `WithModel(new DashScopeModel("qwen-plus", key))`.

**Q: Is the workspace required?**

No. `WithWorkspaceRoot(...)` only enables workspace context injection, `@path` expansion, and memory maintenance; it runs fine without configuration.

**Q: Which middlewares are auto-assembled?**

SandboxLifecycle(50) → Subagents(300) → Teams(500) → Inbox(200) → PlanMode(400) → Compaction(700) → MemoryFlush(800) → AgentTrace(100) → Transcript(900), plus WorkspaceContext(25) / AtPathExpansion(20) / MemoryMaintenance(900) when workspace is configured. See [Middleware](../building-blocks/middleware.md) for the full Order table.

**Q: How to add MCP tools to the agent?**

```csharp
var mcp = McpClientBuilder.Create().UseStdio("node", "mcp.js").Build();
var tools = await new McpManager() { /* RegisterClient */ }.CreateToolsAsync();
```

See [Tools](../building-blocks/tool.md#mcp-client) for details.

## Storage and Extensions

**Q: The `AgentScope.Extensions.Redis` package does not exist?**

2.0 reorganized packages: Redis state store is in `AgentScope.Extensions.Store.Redis` (`RedisAgentStateStore`).

**Q: Is the channel extension `IChannel` the same as Harness's `IChannel`?**

No. `AgentScope.Extensions.Channel.*` implements the umbrella project's `AgentScope.Extensions.Channel.IChannel` (webhook client style, with `OnMessageReceived` event); Harness's internal interface is `AgentScope.Harness.Gateway.Channel.IChannel` (gateway routing style). An adapter is needed for integration.

**Q: What form do Mem0 / Dify and similar extensions take?**

`Mem.*` / `Rag.*` are standalone HTTP client classes (e.g., `Mem0LongTermMemory(http, apiKey, baseUrl?)`), not implementing Core interfaces. They need to be manually adapted to `ILongTermMemory` / RAG layers.
