---
title: "Architecture"
description: "HarnessAgent composition, HarnessAgentBuilder full configuration, and middleware assembly"
---

## HarnessAgent Composition

`HarnessAgent` (`AgentScope.Harness`) composes the inner `EnhancedReActAgent` with various subsystems to provide a complete agent runtime:

```
HarnessAgent
├── EnhancedReActAgent        ← Reasoning-acting loop (AgentScope.Core)
├── IMessageBus               ← Message bus (default WorkspaceMessageBus)
├── IFilesystem               ← Filesystem abstraction (default local sandbox)
├── IGateway                  ← Gateway (HarnessGateway, delegates to inner Agent)
└── List<IHarnessMiddleware>  ← Middleware pipeline (onion model)
```

`HarnessAgent` implements `IAgent`:

- `CallAsync(IReadOnlyList<Msg>, RuntimeContext?)` / `CallAsync(Msg, ...)` / `CallAsync(string, ...)`: calls the inner Agent after passing through the middleware pipeline;
- `StreamEventsAsync(...)`: directly forwards the inner Agent's `IAsyncEnumerable<Event>`;
- `ObserveAsync`, `Interrupt()` / `Interrupt(Msg)`.

`HarnessAgent`'s constructor is internal and can **only be created via `HarnessAgentBuilder`**.

## HarnessAgentBuilder

| Method | Signature | Default | Description |
|------|------|--------|------|
| `WithName` | `(string name)` | `"harness-agent"` | Agent name |
| `WithSystemPrompt` | `(string prompt)` | Built-in English prompt | System prompt |
| `WithModel` | `(IModel model)` | **Required** | Throws if not set on Build |
| `WithToolkit` | `(Toolkit toolkit)` | null | Inject all tools at once |
| `WithPermission` | `(IPermissionEngine)` | null | Permission engine |
| `WithMessageBus` | `(IMessageBus bus)` | `new WorkspaceMessageBus()` | Message bus |
| `WithFilesystem` | `(IFilesystem fs)` | Local current directory filesystem | See [filesystem](./filesystem.md) |
| `WithDefaultFilesystem` | `(string? workspaceRoot = null)` | Current directory | Convenience: local sandbox mode |
| `WithTeamClient` | `(ITeamClient team)` | `new LocalTeamClient()` | Team collaboration client |
| `WithSubagentManager` | `(ISubagentManager mgr)` | `new DefaultAgentManager()` | Subagent manager |
| `WithMiddleware` | `(IHarnessMiddleware mw)` | — | Append custom middleware, can be called multiple times |
| `WithMaxIterations` | `(int n)` | `10` | Max ReAct iterations |
| `WithWorkspace` | `(WorkspaceManager mgr)` | null | Enable workspace triple middleware |
| `WithWorkspaceRoot` | `(string root, bool sandboxed = true)` | — | Convenience overload: `new WorkspaceManager(root, sandboxed)` |
| `WithToolResultEviction` | `(ToolResultEvictionConfig cfg)` | null | Enable large tool result eviction |
| `WithMemoryConsolidator` | `(MemoryConsolidator c)` | null | Memory consolidator (consumed by maintenance middleware) |
| `WithSkillUsageStore` | `(SkillUsageStore store)` | null | Enable skill usage statistics middleware |
| `WithSkillCurator` | `(SkillCurator curator)` | null | Enable skill curation middleware |
| `Build` | `()` | — | Assemble and return `HarnessAgent` |

### Typical Assembly

```csharp
using AgentScope.Harness;
using AgentScope.Harness.Middleware;

HarnessAgent agent = new HarnessAgentBuilder()
    .WithName("coder")
    .WithSystemPrompt("You are a coding assistant.")
    .WithModel(model)
    .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
    .WithMaxIterations(20)
    .WithMiddleware(new CompactionMiddleware(maxContextLength: 8192))
    .Build();
```

## Build() Assembly Details

`Build()` does four things:

1. **Build the inner `EnhancedReActAgent`**: applies `Name` / `SysPrompt` / `Model` / tools / permission / `MaxIterations`;
2. **Create `HarnessGateway`** wrapping the inner Agent;
3. **Assemble the middleware pipeline**: first add user-provided middlewares via `WithMiddleware`, then automatically append `SandboxLifecycle` → `Subagents` → `Teams` → `Inbox` → `PlanMode` → `Compaction` → `MemoryFlush` → `AgentTrace` → `Transcript`; when workspace is configured, append `WorkspaceContext` / `AtPathExpansion` / `MemoryMaintenance`; when eviction / skill stats / skill curation are explicitly configured, append corresponding middlewares;
4. **Construct `HarnessAgent`**.

At runtime, middlewares form an onion chain sorted by `Order` in ascending order (see [Middleware](../building-blocks/middleware.md)). The system prompt is first rewritten by each layer's `OnSystemPromptAsync` in sequence, then written back to the inner Agent.

## Gateway

`IGateway` exposes the Agent as a unified entry point:

```csharp
public interface IGateway
{
    Task<Msg> RunAsync(Msg input, RuntimeContext? context = null, CancellationToken ct = default);
    IAsyncEnumerable<Event> RunStreamAsync(Msg input, RuntimeContext? context = null, CancellationToken ct = default);
}
```

`HarnessGateway(IAgent agent)` is the default implementation, directly delegating to the inner Agent's `CallAsync` / `StreamEventsAsync`. Channel (see [Channel](./channel.md)) interacts with the Agent through the gateway.

## Message Bus (IMessageBus)

`WorkspaceMessageBus` (default) is based on `System.Threading.Channels` and supports four modes:

- **Drain queue**: `QueuePushAsync(queue, entry)` / `QueueDrainAsync(queue)` / `QueueDeleteAsync`;
- **Replay log**: `LogAppendAsync(log, entry)` / `LogReadAsync(log, startSeq)` / `LogTrimAsync`;
- **Publish-Subscribe**: `PublishAsync(topic, entry)` / `Subscribe(topic, handler)`;
- **Inbox domain helper**: `InboxPushAsync(agentId, entry)` / `InboxDrainAsync(agentId)` (consumed by `InboxMiddleware` at the start of each turn).

Entry type `BusEntry(Id, Key, Payload)`, with monotonic `Sequence`.

## Related Documentation

- [Middleware](../building-blocks/middleware.md) —— Middleware interface and Order table
- [Filesystem](./filesystem.md) · [Workspace](./workspace.md) · [Subagent](./subagent.md) · [Channel](./channel.md)
