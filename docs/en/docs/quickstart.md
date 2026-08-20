---
title: "Quickstart"
description: "Get started quickly with AgentScope .NET 2.0 — Run your first agent with HarnessAgent"
---

## Installation

AgentScope .NET is built on **.NET 10.0** (`net10.0`). The dotnet CLI is recommended.

### NuGet Packages

`AgentScope.Harness` is the recommended entry package. It internally references the core package `AgentScope.Core` and bundles workspace management, message bus, filesystem abstraction, sub-agents, middleware pipeline, and other engineering capabilities into a single Builder:

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Harness" Version="2.0.1" />
</ItemGroup>
```

If you only need the bare `ReActAgent` / `EnhancedReActAgent` framework API (without workspace / middleware pipeline / sub-agents), reference `AgentScope.Core` directly:

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Core" Version="2.0.1" />
</ItemGroup>
```

:::{note}
Unlike many frameworks, **all model providers (OpenAI / DashScope / Anthropic / Gemini / DeepSeek / Ollama / Mock) are built into `AgentScope.Core`**; there are no `AgentScope.Extensions.Model.*` model extension packages. No additional packages are needed to integrate a model.
:::

## First Agent

The following example is consistent with `examples/AgentScope.Lab/Program.cs` in the repository. It uses `HarnessAgent` to accomplish three things: **build a HarnessAgent**, **identify sessions via RuntimeContext**, and **multi-turn conversation**.

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Harness;
using AgentScope.Harness.Middleware;

class FirstAgent
{
    static async Task Main(string[] args)
    {
        // Model: construct directly, ApiKey usually from environment variables
        IModel model = new DashScopeModel(
            "qwen-plus",
            Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"));

        HarnessAgent agent = new HarnessAgentBuilder()
            .WithName("note-taker")
            .WithSystemPrompt("You are an assistant that helps users take notes.")
            .WithModel(model)
            .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
            .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))
            .Build();

        // Runtime context: record type, derive new instances with With* methods
        RuntimeContext ctx = RuntimeContext.Empty
            .WithUserId("alice")
            .WithSessionId("demo-session");

        Msg first = Msg.Builder()
            .Role("user")
            .TextContent("My name is Tianyu. I'm preparing a tech talk about ReAct today.")
            .Build();
        Msg reply1 = await agent.CallAsync(first, ctx);
        Console.WriteLine($"Assistant: {reply1.GetTextContent()}");

        Msg second = Msg.Builder()
            .Role("user")
            .TextContent("What is my name? What am I doing today?")
            .Build();
        Msg reply2 = await agent.CallAsync(second, ctx);
        Console.WriteLine($"Assistant: {reply2.GetTextContent()}");
    }
}
```

When no API Key is available, use `MockModel` to walk through the flow (echoes input without making network requests):

```csharp
IModel model = MockModel.Builder().ModelName("mock-model").Build();
```

### Key API Reference

| API | Description |
|-----|-------------|
| `new HarnessAgentBuilder()...Build()` | `HarnessAgent` can only be created via `HarnessAgentBuilder` (constructor is internal) |
| `.WithModel(IModel)` | Required; accepts any `IModel` implementation, **no string model ID overload** |
| `.WithWorkspaceRoot(path)` | Convenience overload, equivalent to `WithWorkspace(new WorkspaceManager(root, sandboxed: true))`; when set, automatically enables workspace context injection, `@path` expansion, and memory maintenance middlewares |
| `.WithMiddleware(IHarnessMiddleware)` | Appends custom middleware to the pipeline (can be called multiple times); a set of built-in middlewares are auto-configured |
| `RuntimeContext.Empty.WithUserId(...).WithSessionId(...)` | `RuntimeContext` is an immutable record, no Builder class |
| `agent.CallAsync(Msg, RuntimeContext)` | Drives one reasoning-action loop, returns the final `Msg`; `reply.GetTextContent()` extracts text |

See [Harness Architecture](./harness/architecture.md) for all available Builder methods.

### Streaming Reasoning and Tool Calls

Replace `CallAsync(...)` with `StreamEventsAsync(...)` to get real-time intermediate events such as reasoning fragments and tool calls, suitable for Web / TUI rendering. It returns an `IAsyncEnumerable<Event>`, consumed with `await foreach`:

```csharp
using AgentScope.Core.Events;

await foreach (Event evt in agent.StreamEventsAsync(
    Msg.Builder().Role("user").TextContent("List three key points for today.").Build(), ctx))
{
    if (evt.Type == EventType.ReasoningChunk && evt.Message != null)
    {
        // Streaming text fragments from the model output
        Console.Write(evt.Message.GetTextContent());
    }
    else if (evt.Type == EventType.ToolCallStart)
    {
        Console.WriteLine("\n[tool] Model requested a tool call");
    }
    else if (evt.IsLast)
    {
        Console.WriteLine("\n[done]");
    }
}
```

Event types are defined by the `AgentScope.Core.Events.EventType` enum: `ReasoningStart/Chunk/Finish`, `ToolCallStart/Chunk/Finish`, `ActingStart/Chunk/Finish`, `SummaryStart/Chunk/Finish`, `Error`. See [Message and Event](./building-blocks/message-and-event.md) for the full description.

### Multi-User Concurrency

A `HarnessAgent` instance can be reused. Pass different `UserId` / `SessionId` values via `RuntimeContext` — each call operates independently:

```csharp
// Create a single agent instance at application startup (singleton)
HarnessAgent agent = new HarnessAgentBuilder()
    .WithName("note-taker")
    .WithSystemPrompt("You are an assistant that helps users take notes.")
    .WithModel(model)
    .Build();

// In an HTTP handler — different requests pass different RuntimeContext
await agent.CallAsync(userInput, RuntimeContext.Empty
    .WithUserId(userId)
    .WithSessionId(sessionId));
```

`HarnessAgent` also provides a plain-text convenience overload: `agent.CallAsync("hello", ctx)` internally wraps the string into a `Msg`.

To restore sessions across processes, use `EnhancedReActAgent`'s `SaveTo` / `LoadFrom` with `SessionManager`, or configure `StateBackedMemory` + `IAgentStateStore` for memory. See [Context and AgentState](./building-blocks/context.md).

## Next Steps

- [Agent](./building-blocks/agent.md) — Full Builder, invocation, streaming, and structured output for `EnhancedReActAgent`
- [Model](./building-blocks/model.md) — Constructor signatures and streaming interfaces for each provider's model class
- [Tool](./building-blocks/tool.md) — `[Tool]` attribute registration, `Toolkit`, MCP client
- [Harness Architecture](./harness/architecture.md) — Complete `HarnessAgentBuilder` methods and middleware pipeline
- [Workspace](./harness/workspace.md) — Directory layout and loading mechanism for `AGENTS.md` / `MEMORY.md` / `skills/`
