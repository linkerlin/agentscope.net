---
title: "Quickstart"
description: "Get started with AgentScope .NET 2.0 — bring up your first long-running agent with HarnessAgent"
---

## Installation

AgentScope .NET requires .NET 8.0 SDK or newer. The dotnet CLI is recommended.

### NuGet package

`HarnessAgent` is the recommended entry point — it packages workspace, long-term memory, session persistence, subagents, sandboxes, and other engineering capabilities into one builder. Depending on `AgentScope.Harness` pulls `AgentScope.Core` in transitively:

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Harness" Version="$(AgentScopeVersion)" />
</ItemGroup>
```

:::{note}
Substitute `$(AgentScopeVersion)` with the latest version. See [Release Notes](others/release-notes.md) for the latest version and full release details.
:::

If you only need the bare `ReActAgent` APIs (no workspace / persistence / subagents / sandbox), `AgentScope.Core` is enough for the agent framework itself. Concrete model providers are separate: provider-specific chat models and formatters live in independent `AgentScope.Extensions.Model.*` packages. The difference between `ReActAgent` and `HarnessAgent` is covered in [Harness Architecture](./harness/architecture.md).

The quickstart below uses DashScope through `.Model("dashscope:qwen-plus")`, so add the matching model extension as well:

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Model.DashScope" Version="$(AgentScopeVersion)" />
</ItemGroup>
```

MCP integration requires the official MCP SDK — see `AgentScope.Examples/AgentScope.Examples.csproj` for a working example.

## Your first agent

The example below uses `HarnessAgent` to demonstrate three things at once: **workspace-driven persona** (`AGENTS.md`), **automatic session persistence** (the second turn with the same `sessionId` remembers the first), and **conversation compaction** (over-threshold compaction + long-term facts distilled into `MEMORY.md`). The model id is passed as a string to `.Model(...)` — `ModelRegistry` resolves it and reads the matching API-key env var automatically.

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Harness.Agent;
using AgentScope.Harness.Agent.Memory.Compaction;

class FirstAgent
{
    static void Main(string[] args)
    {
        HarnessAgent agent = HarnessAgent.CreateBuilder()
                .Name("note-taker")
                .SysPrompt("You are a note-taking assistant.")
                // String form resolved via ModelRegistry — picks up DASHSCOPE_API_KEY
                // from the environment. Use "openai:gpt-5.5", "anthropic:claude-sonnet-4-5",
                // "gemini:gemini-2.0-flash", or "ollama:llama3" to switch providers.
                .Model("dashscope:qwen-plus")
                .Workspace(Path.GetFullPath(".agentscope/workspace"))
                .Compaction(CompactionConfig.CreateBuilder()
                        .TriggerMessages(30)
                        .KeepMessages(10)
                        .Build())
                .Build();

        RuntimeContext ctx = RuntimeContext.CreateBuilder()
                .SessionId("demo-session")
                .UserId("alice")
                .Build();

        // Turn 1: introduce yourself + state today's task
        agent.CallAsync(new UserMessage("My name is Alice, and I'm preparing a tech talk on ReAct today."), ctx).GetAwaiter().GetResult();

        // Turn 2: same sessionId — state from turn 1 is restored automatically
        agent.CallAsync(new UserMessage("What is my name? What am I doing today?"), ctx).GetAwaiter().GetResult();
    }
}
```

After this run you get two directory trees — the **workspace** and the **state store**:

```
.agentscope/workspace/                          ← workspace (agent content)
├── AGENTS.md                                   ← write one to give the agent its persona (optional)
└── agents/note-taker/
    └── sessions/                               ← never-compacted raw conversation log

~/.agentscope/state/note-taker/                 ← state store (outside workspace)
└── alice/demo-session/                         ← AgentState auto-saved / auto-loaded
    └── agent_state.json
```

`AgentState` lives **outside the workspace** at `~/.agentscope/state/<agentId>/` by default — because state is a prerequisite for restoring the workspace itself (e.g. after a sandbox wipe), so it must not be entangled with workspace data. Restart the process with the same `sessionId` and the second turn still remembers the first.

:::{warning}
The default `JsonFileAgentStateStore` is a local-file backend suitable for development and single-node deployment. For production clusters, use a distributed implementation such as `RedisAgentStateStore` (provided by `AgentScope.Extensions.Redis`) or implement your own `IAgentStateStore`. See [Going to Production](./others/going-to-production.md).
:::

After enough turns trip compaction, distilled facts first land in `workspace/memory/YYYY-MM-DD.md`, then a throttled background job merges them into `MEMORY.md`, which is injected into the system prompt on the next reasoning step.

### Streaming reasoning and tool calls

Swap `CallAsync(...)` for `StreamEventsAsync(...)` to receive incremental events — text deltas, tool calls, etc. — suitable for Web / TUI rendering:

```csharp
using AgentScope.Core.Event;

agent.StreamEventsAsync(new UserMessage("Summarize today in three bullets."))
        .Subscribe(event =>
        {
            if (event.Type == AgentEventType.TextBlockDelta)
            {
                // Streaming text fragment — append to UI or stdout
                Console.Write(((TextBlockDeltaEvent)event).Delta);
            }
            else if (event.Type == AgentEventType.ToolCallStart)
            {
                // The agent is about to call a tool — surface the call info
                Console.WriteLine("\n[tool] " + ((ToolCallStartEvent)event).ToolCallName);
            }
            // Other events: thinking blocks, tool results, reply end, etc.
        });
```

:::{tip}
Set `DASHSCOPE_API_KEY` in the environment before running. To switch providers, add the matching `AgentScope.Extensions.Model.*` package, change the string passed to `.Model(...)`, and export the matching API key (`OPENAI_API_KEY`, `ANTHROPIC_API_KEY`, `GEMINI_API_KEY`). When you need explicit control over timeouts or custom endpoints, build the model with the provider builder such as `DashScopeChatModel.CreateBuilder()...Build()` and pass it to `.Model(Model)` instead.
:::

### Multi-user concurrency

The agent is **stateless between calls** — a single instance can handle requests from different users and sessions. Pass `userId` / `sessionId` via `RuntimeContext` and the agent automatically loads and isolates the corresponding conversation state:

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Harness.Agent;
using AgentScope.Harness.Agent.Memory.Compaction;

// Create one agent instance at startup (singleton is fine)
HarnessAgent agent = HarnessAgent.CreateBuilder()
        .Name("note-taker")
        .SysPrompt("You are a note-taking assistant.")
        .Model("dashscope:qwen-plus")
        .Workspace(Path.GetFullPath(".agentscope/workspace"))
        .Compaction(CompactionConfig.CreateBuilder()
                .TriggerMessages(30)
                .KeepMessages(10)
                .Build())
        .Build();

// In your HTTP handler — different requests pass different RuntimeContexts
agent.CallAsync(new UserMessage(userInput), RuntimeContext.CreateBuilder()
        .SessionId(sessionId)
        .UserId(userId)
        .Build()).GetAwaiter().GetResult();
```

Calls targeting the same `(userId, sessionId)` are automatically serialized (no concurrent writes to one session); calls to different sessions run in parallel. For full production patterns (Redis session, sandbox, skill repositories), see [Going to Production](./others/going-to-production.md).

## Next steps

- [Agent](./building-blocks/agent.md) — full `ReActAgent` API, builder fields, `CallAsync` / `StreamEventsAsync` / `Observe`, human-in-the-loop, `IAgentStateStore` configuration
- [Harness Architecture](./harness/architecture.md) — how `HarnessAgent`'s capabilities cooperate, how state flows
- [Workspace](./harness/workspace.md) — `AGENTS.md` / `MEMORY.md` / `skills/` / `subagents/` / `tools.json` directory layout and loading model
- [Filesystem](./harness/filesystem.md) — local + shell / shared store / sandbox deployment modes
