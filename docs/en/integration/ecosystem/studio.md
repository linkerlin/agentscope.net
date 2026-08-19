# AgentScope Studio

`AgentScope.Extensions.Studio` integrates Agents with [AgentScope Studio](https://github.com/agentscope-ai/agentscope-studio): every Agent invocation is pushed to Studio for visual debugging, trace replay, and human-in-the-loop input.

## When to use

- You want to inspect event streams, reasoning, and tool calls in Studio during development.
- You need to issue `RequestUserInput` from Studio and let a real user respond.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Studio" Version="$(AgentScopeVersion)" />
```

## Quickstart

```csharp
using AgentScope.Core.Studio;

// 1) Initialize Studio connection (HTTP + WebSocket)
StudioManager.Init()
    .StudioUrl("http://localhost:8000")
    .Project("MyProject")
    .RunName("experiment_001")
    .Initialize()
    .Wait();

// 2) Attach StudioMessageHook so messages are pushed to Studio
ReActAgent agent = ReActAgent.Builder()
    .Name("Assistant")
    .Model(model)
    .Hook(new StudioMessageHook(StudioManager.GetClient()))
    .Build();

// 3) Use the Agent normally; Studio mirrors the conversation
agent.Call(msg).Wait();
```

## What Studio gives you

- **Message push**: every user / assistant / tool message is mirrored to Studio.
- **Traces**: Studio organizes events into a trace tree per `RunName`.
- **Human-in-the-loop**: via `StudioUserAgent` or `RequestUserInput`, Studio's UI prompts a real user to fill in input before execution continues.

## API overview

| Class | Purpose |
| --- | --- |
| `StudioManager` | Singleton entry point — initialize and access clients |
| `StudioConfig` | URL / project / runName configuration |
| `StudioClient` | HTTP client for events, messages, and run registration |
| `StudioWebSocketClient` | WebSocket client for inbound commands (e.g. user input) |
| `StudioMessageHook` | A `Hook` for `ReActAgent` that auto-pushes `Msg` |
| `StudioUserAgent` | "Human-played" Agent that blocks on Studio user input |

## When to disable

In production, you usually don't want this hook attached (every call writes to Studio). Gate it via configuration:

```csharp
// Only enable when configuration is present
if (configuration.GetSection("AgentScope:Studio").Exists())
{
    StudioManager.Init()
        .StudioUrl(url)
        .Project(project)
        .Initialize()
        .Wait();
    services.AddSingleton(new StudioMessageHook(StudioManager.GetClient()));
}
```
