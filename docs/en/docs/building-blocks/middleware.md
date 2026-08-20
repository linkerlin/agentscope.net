---
title: "Middleware"
description: "Core MiddlewareBase / Hook and Harness IHarnessMiddleware pipeline"
---

## Overview

AgentScope .NET has two complementary interception mechanisms:

| Mechanism | Package | Hook Points | Applicable To |
|-----------|---------|-------------|---------------|
| **Hook** (`IHook` + `HookManager`) | `AgentScope.Core` | Before/after reasoning / acting / summary and streaming chunks | Observation and termination of the ReAct loop |
| **IHarnessMiddleware** | `AgentScope.Harness` | Turn (OnAgent) / model call / tool execution / system prompt | Wraps the entire agent turn (onion model) |

See [Agent — Hook System](./agent.md#hook-system) for Hook usage. This document focuses on `IHarnessMiddleware`.

## IHarnessMiddleware

```csharp
public interface IHarnessMiddleware
{
    int Order { get; }   // Lower values execute first

    ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default);
    ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default);
    ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default);

    // Optional: rewrite system prompt (returns the original by default)
    ValueTask<string> OnSystemPromptAsync(MiddlewareContext ctx, string prompt, CancellationToken ct = default);
}
```

### MiddlewareContext

| Property | Type | Description |
|----------|------|-------------|
| `AgentName` | `string` | Name of the called agent |
| `Model` | `string?` | Model identifier (optional) |
| `ToolName` | `string?` | Tool name (optional) |
| `Messages` | `List<Msg>` | Current turn message list (writable) |
| `ToolCalls` | `List<ToolUseBlock>` | Tool calls to execute in this turn |
| `Runtime` | `RuntimeContext?` | Runtime context |
| `UserId` / `SessionId` | `string` | Computed properties from `Runtime` |
| `Items` | `Dictionary<string, object?>` | Key-value store shared between middlewares (e.g., `filesystem`, `bus`, `session_id`, `plan_mode`, `needs_compaction`) |

### Execution Model (Onion Model)

Before `HarnessAgent.CallAsync` calls the inner `EnhancedReActAgent`:

1. Sorts all middlewares by `Order` in ascending order;
2. Calls `OnSystemPromptAsync` on each middleware to rewrite the system prompt and writes it back to the inner agent;
3. Executes the `OnAgentAsync` chain in onion fashion — each middleware can do work before/after `next()`, or short-circuit the entire turn by not calling `next()` (when short-circuited, the framework falls back to executing the core call directly to maintain call semantics).

## Custom Middleware

```csharp
using AgentScope.Harness.Middleware;

public sealed class AuditMiddleware : IHarnessMiddleware
{
    public int Order => 50;   // See the built-in Order table below

    public async ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await next();                          // Invoke the rest of the chain
        sw.Stop();
        Console.WriteLine($"[{ctx.AgentName}] Turn took {sw.ElapsedMilliseconds}ms, session {ctx.SessionId}");
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
    {
        Console.WriteLine($"[tool] {ctx.ToolName}");
        return next();
    }
}

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithMiddleware(new AuditMiddleware())     // Can be called multiple times, sorted by Order
    .Build();
```

## Built-in Middleware Overview

`HarnessAgentBuilder.Build()` automatically assembles the following middlewares (no manual addition required). Custom middlewares added via `WithMiddleware` are sorted together with them by `Order`:

| Order | Middleware | Function |
|-------|-----------|----------|
| 20 | `AtPathExpansionMiddleware(WorkspaceManager)` | Expands `@path` references into `<attached_file>` tags (max 1000 lines) |
| 25 | `WorkspaceContextMiddleware(WorkspaceManager, agentName, ...)` | Injects workspace context / domain knowledge / memory into the system prompt (token budget 8000) |
| 30 | `ToolResultEvictionMiddleware(IFilesystem, ToolResultEvictionConfig?)` | Offloads oversized tool results to disk and replaces them with placeholders |
| 50 | `SandboxLifecycleMiddleware(SandboxManager?)` | Injects sandbox context |
| 100 | `AgentTraceMiddleware` | Records turn start / end and duration |
| 200 | `InboxMiddleware(IMessageBus)` | Drains inbox messages before each turn |
| 300 | `SubagentsMiddleware(ISubagentManager)` | Injects sub-agent manager |
| 400 | `PlanModeMiddleware` | Appends planning instructions to the system prompt in Plan mode |
| 500 | `TeamsMiddleware(ITeamClient)` | Injects team client |
| 700 | `CompactionMiddleware(int maxContextLength = 4096)` | Marks `needs_compaction` when context exceeds the limit |
| 760 | `SkillUsageMiddleware(SkillUsageStore)` | Tracks skill view / usage counts |
| 780 | `SkillCuratorMiddleware(SkillCurator)` | Triggers skill curation in the background after a turn |
| 800 | `MemoryFlushMiddleware` | Marks memory to be flushed after a turn |
| 900 | `TranscriptMiddleware(ITranscriptStore)` | Records turn transcripts |
| 900 | `MemoryMaintenanceMiddleware(WorkspaceManager, MemoryConsolidator?, ...)` | Periodically archives logs and consolidates memory |

Among these, `CompactionMiddleware`, `TranscriptMiddleware`, etc. are assembled on every Build; the workspace-related trio (`WorkspaceContext` / `AtPathExpansion` / `MemoryMaintenance`) are enabled only after `WithWorkspace(...)` is configured; `ToolResultEviction`, `SkillUsage`, `SkillCurator` require explicit component configuration.

## Related Documentation

- [Agent](./agent.md) — Core layer Hook system
- [Harness Architecture](../harness/architecture.md) — Middleware assembly in HarnessAgent
