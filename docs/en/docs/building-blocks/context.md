---
title: "Context & AgentState"
description: "Stateless agent engine, AgentState lifecycle, state persistence, and RuntimeContext"
---

## Stateless Agent Engine

`ReActAgent` (and `HarnessAgent` that wraps it) is designed as a **stateless engine**: the agent instance itself holds only immutable configuration — system prompt, model, tools, middleware chain — while all per-session mutable data lives in `AgentState`, indexed by `(UserId, SessionId)`. A single agent instance can concurrently serve many users and sessions; the caller simply passes a different `RuntimeContext` on each `CallAsync()`.

```
┌──────────────────────────────────────────────────────────────────┐
│                     HarnessAgent (singleton)                     │
│  Immutable config: sysPrompt, model, toolkit, middlewares        │
│                                                                  │
│  ┌─ state cache ─────────────────────────────────────────────┐   │
│  │  ("alice","s1") → AgentState  ← CallAsync(…, RC(alice,s1))   │
│  │  ("bob","s2")   → AgentState  ← CallAsync(…, RC(bob,s2))    │
│  └───────────────────────────────────────────────────────────┘   │
│                                                                  │
│  per-session gate: same (uid,sid) calls serialised, others ∥     │
└──────────────────────────────────────────────────────────────────┘
```

### What this means for you

- **No agent-per-user registry.** One `HarnessAgent` instance can serve all your users — just vary `RuntimeContext.UserId` and `RuntimeContext.SessionId` per request.
- **Concurrency is built in.** Different `(UserId, SessionId)` pairs run fully in parallel; the same pair is automatically serialised to preserve conversation consistency.
- **State is fully internal.** The agent loads `AgentState` from the store at call entry and saves it at call exit — the caller never manages state objects directly.
- **Per-call isolation.** Each `CallAsync()` works on its own `AgentState` snapshot. Middleware and tools access the call-scoped state via `RuntimeContext.GetAgentState()` (injected by the framework at call entry), so concurrent calls never see each other's state.

---

## AgentState

An [`IAgentStateStore`](../../integration/session/index.md) persists an **`AgentState`** (`AgentScope.Core.State.AgentState`) — a complete snapshot of everything that makes the agent restartable:

| `AgentState` field | Content |
|---|---|
| `GetSessionId()` | The session identifier this state belongs to |
| `GetUserId()` | The user identifier (nullable for anonymous sessions) |
| `GetContext()` / `ContextMutable()` | Current conversation history (user / assistant / tool calls / tool results) |
| `GetSummary()` | Compacted summary (when compaction is enabled) |
| `GetPermissionContext()` | Tool permission rules — see [Permissions](./permission-system.md) |
| `GetPlanModeContext()` | Whether Plan Mode is active, current plan file path |
| `GetTasksContext()` | The `todo_write` task list |
| `GetToolContext()` | Active toolkit groups (`ActivatedGroups`) |

`AgentState` also carries a transient, non-serialised `InterruptControl` for per-session interrupt signalling — see [Per-session interrupt](#per-session-interrupt) below.

At the end of each `CallAsync()`, the framework writes the entire `AgentState` to the state store under the key `agent_state`, addressed by the call's `(UserId, SessionId)`. The next `CallAsync()` with the same `(UserId, SessionId)` loads it back automatically. **Provided the state store is distributed (e.g. Redis), agent instances on different processes — even different physical machines — see identical state.**

### The auto-persistence and recovery flow

```
CallAsync(msgs, RuntimeContext(userId, sessionId))
  │
  ├─ per-session gate: serialise same (uid, sid), others run in parallel
  │
  ▼
  load AgentState from cache or stateStore
  │   inject onto RuntimeContext: rc.SetAgentState(state)
  │
  ▼
  reasoning loop
  │   middlewares mutate state.ContextMutable()
  │   (compaction, Plan, todo_write, permissions, …)
  │
  ▼
  save AgentState
  │   stateStore.Save(userId, sessionId, "agent_state", state)
  │
  ▼
  return result
```

This wiring lives in `ReActAgent` itself; `HarnessAgent` inherits it for free. The agent instance holds no fixed session — each call reads / writes the slot named by its `RuntimeContext` (falling back to the builder-time `DefaultSessionId`).

> Mid-`CallAsync()` state changes happen against the in-memory `AgentState`. **The state store is written once per call (and on shutdown), not on every message** — so the throughput pressure on your store stays low.

### Built-in and extension implementations

Anything implementing `AgentScope.Core.State.IAgentStateStore` works. Pick by deployment shape:

| Implementation | Module | Use case |
|---|---|---|
| `InMemoryAgentStateStore` | `AgentScope.Core` | Unit tests / single-process demos; lost on exit |
| `JsonFileAgentStateStore` | `AgentScope.Core` | Local dev with file persistence; not cross-node. **`HarnessAgent` default**, rooted at `~/.agentscope/state/<agentId>/` (override the base via the `AGENTSCOPE_STATE_HOME` environment variable); **single-host** |
| `RedisAgentStateStore` | `AgentScope.Extensions.Redis` | **Production default** for multi-replica deployments; supports StackExchange.Redis / ServiceStack.Redis (Standalone / Cluster / Sentinel) |
| `MysqlAgentStateStore` | `AgentScope.Extensions.MySql` | When state needs to flow into a relational store (audit, reporting) |

Switching is one call at builder time:

```csharp
// Default (single host) — omit .StateStore(...); a local JsonFileAgentStateStore is used automatically
HarnessAgent agent = HarnessAgent.Builder()
    .Name("MyAgent")
    .Model(model)
    .Workspace(workspace)
    .Build();

// Production multi-replica — use DistributedStore
ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("redis://redis.prod:6379");
HarnessAgent agent = HarnessAgent.Builder()
        .Name("MyAgent")
        .Model(model)
        .Workspace(workspace)
        .StateStore(new RedisAgentStateStore(redis))
        .DistributedStore(RedisDistributedStore.FromConnectionMultiplexer(redis))
        .Build();
```

:::{warning}
The built-in `JsonFileAgentStateStore` / `InMemoryAgentStateStore` are single-host only. If you've already chosen `Filesystem(SandboxFilesystemSpec)` or `Filesystem(RemoteFilesystemSpec)` (distributed workspace), HarnessAgent **rejects** a local state store at build time with `InvalidOperationException` — sandbox state must be shared across replicas. Configure a distributed store via `.DistributedStore(...)` (e.g. `RedisDistributedStore`) or `.StateStore(...)`.
:::

### Real-time resume across processes and machines

Once the state store is distributed (e.g. Redis), cross-machine resume is **automatic**:

```csharp
// Node A — start a conversation
HarnessAgent agentA = HarnessAgent.Builder()
    .StateStore(redisStore)
    /* ... */ .Build();
await agentA.CallAsync(msg, RuntimeContext.Builder()
    .SessionId("alice-2026-06-02-001")
    .UserId("alice")
    .Build());

// Node B — different physical machine, separate process
HarnessAgent agentB = HarnessAgent.Builder()
    .StateStore(redisStore)
    /* same state store */ .Build();

// Node B's first CallAsync() with the same (userId, sessionId) loads the AgentState node A left in Redis
await agentB.CallAsync(nextMsg, RuntimeContext.Builder()
    .SessionId("alice-2026-06-02-001")
    .UserId("alice")
    .Build());
```

This buys you:

- **Failover**: a crashed node — conversations migrate to a healthy one, user notices nothing.
- **Rolling deploys**: old pods save on shutdown, new pods load on first call — **conversations never break across releases**.
- **Cross-surface continuity**: a user starts in the Web UI, switches to the CLI — same `(UserId, SessionId)`, all memory present.

The `(UserId, SessionId)` pair defines the namespacing: `SessionId` alone is enough for most cases; add `UserId` when you need per-user partitioning.

### Multi-user isolation

`SessionId` and `UserId` solve different problems:

- **`SessionId`** — which conversation this is; independent `AgentState` snapshot.
- **`UserId`** — which user owns this conversation; also drives which user's namespace files land in, see [Filesystem](../harness/filesystem.md).

```csharp
await agent.CallAsync(msg, RuntimeContext.Builder()
    .SessionId("alice-1").UserId("alice").Build());

await agent.CallAsync(msg, RuntimeContext.Builder()
    .SessionId("bob-1").UserId("bob").Build());
```

Two users — separate state, separate filesystem paths, no crosstalk. For `AgentState`-level user isolation in production, set `UserId` on the `RuntimeContext`: the store addresses each slot by `(UserId, SessionId)` (with `RedisAgentStateStore` the `UserId` becomes part of the Redis key) rather than relying on filesystem path bucketing.

### Reading and writing `AgentState` directly

When you need to bypass the agent loop (admin console, audit, batch migration):

```csharp
using AgentScope.Core.State;

AgentState state = agent.GetAgentState("alice", "session-001");
Console.WriteLine("messages: " + state.GetContext().Count);

string json = state.ToJson();
AgentState restored = AgentState.FromJsonString(json);
```

| Method | Description |
|------|------|
| `GetContext()` | Current conversation history (immutable view) |
| `ContextMutable()` | Writable view, use with care |
| `SetSummary(...)` / `GetSummary()` | Custom compaction summary (for your own compaction middleware) |
| `ToJson()` / `FromJsonString(string)` | Serialize / deserialize |

### Clearing a session's conversation context

To let a user start a fresh topic without creating a new session, call `ClearContext`. It keeps the
same `(UserId, SessionId)` and preserves non-conversation state such as permissions, tools, tasks,
and Plan Mode. It clears the model-visible message buffer and compaction summary, then immediately
persists the result when the agent has an `IAgentStateStore`.

```csharp
agent.ClearContext("alice", "session-001");

// Or use the same RuntimeContext used by calls.
agent.ClearContext(RuntimeContext.Builder()
    .UserId("alice")
    .SessionId("session-001")
    .Build());
```

Call it after the session's current request has completed. It does not cancel an in-flight call;
the next call starts with the cleared conversation context.

:::{note}
The 1.0 `IMemory` interface (`InMemoryMemory` / `LongTermMemory`, etc.) is `[Obsolete(forRemoval: true)]` in 2.0. New code should use `AgentState.GetContext()` + an `IAgentStateStore`; `IMemory` remains only as a source-compat shim.
:::

### Per-session interrupt

Each `AgentState` carries a transient `InterruptControl` (`AgentScope.Core.Interruption.InterruptControl`) — a per-session interrupt signal that is **never serialised** to the state store (marked `[JsonIgnore]` on `AgentState`). This allows targeted interruption of a single session's in-flight call without affecting other concurrent calls on the same agent instance.

```csharp
// Interrupt a specific session — only that session's call observes the signal
agent.Interrupt("alice", "session-001");

// Interrupt with an injected user message
agent.Interrupt("alice", "session-001", Msg.UserMsg("Please stop and summarise."));
```

The reasoning loop checks `state.InterruptControl.IsInterrupted()` before each iteration. When triggered, the loop enters the `HandleInterrupt` path, which saves state and returns the partial result.

The legacy no-arg `Interrupt()` still works for single-session scenarios — it routes to the currently active session's `InterruptControl`.

:::{note}
`InterruptControl` is a runtime-only signal; it is never persisted. If a session resumes on a different node after failover, the interrupt flag starts cleared. The separate `AgentState.ShutdownInterrupted` flag (which **is** persisted) records whether the session was interrupted by graceful shutdown — the agent can detect and recover from that on next load.
:::

### Concurrent usage

Because the agent is a stateless engine, a single instance handles concurrent requests naturally:

```csharp
HarnessAgent agent = HarnessAgent.Builder()
    .Name("SharedAssistant")
    .Model(model)
    .Workspace(workspace)
    .StateStore(redisStore)
    .Build();

// Different users — fully parallel, no contention
Task<Msg> aliceCall = agent.CallAsync(aliceMsg, RuntimeContext.Builder()
    .UserId("alice").SessionId("s1").Build());
Task<Msg> bobCall = agent.CallAsync(bobMsg, RuntimeContext.Builder()
    .UserId("bob").SessionId("s2").Build());

await Task.WhenAll(aliceCall, bobCall);  // both run in parallel

// Same user, same session — automatically serialised
Task<Msg> call1 = agent.CallAsync(msg1, RuntimeContext.Builder()
    .UserId("alice").SessionId("s1").Build());
Task<Msg> call2 = agent.CallAsync(msg2, RuntimeContext.Builder()
    .UserId("alice").SessionId("s1").Build());

// call2 queues behind call1 — conversation history stays consistent
await Task.WhenAll(call1, call2);
```

**Concurrency rules:**
- **Different `(UserId, SessionId)`** → fully parallel, each call works on its own `AgentState`.
- **Same `(UserId, SessionId)`** → per-session async gate serialises calls in FIFO order — state consistency guaranteed without external locking.
- **`Interrupt(userId, sessionId)`** → targets exactly one session, other in-flight calls unaffected.

:::{tip}
The in-memory state cache grows with the number of distinct sessions a single agent instance has served. For most deployments (hundreds of sessions) this is negligible. For very large-scale scenarios (millions of sessions per process), consider an agent factory pattern with bounded instance pools — but this is rarely needed since `AgentState` objects are lightweight.
:::

---

## `RuntimeContext` — per-call metadata

`RuntimeContext` (in `AgentScope.Core.Agent`) is a lightweight per-call carrier passed to `agent.CallAsync(msgs, ctx)`; hooks and tools share it for the duration of one call. Its free-form / typed attributes are **not persisted**; its `SessionId` / `UserId` fields select which `AgentState` slot the state store loads and saves for this call. At call entry, the framework injects the call-scoped `AgentState` onto the `RuntimeContext` so that middleware, tools, and hooks can access the correct per-call state via `ctx.GetAgentState()`.

```csharp
using AgentScope.Core.Agent;

RuntimeContext ctx = RuntimeContext.Builder()
        .UserId("alice")
        .SessionId("s-001")
        .Put("request_id", "req-2026-06-01-abc")
        .Put<MyTenantInfo>(new MyTenantInfo("tenant-7"))
        .Build();

Msg result = await agent.CallAsync(new List<Msg> { new UserMessage("Hi") }, ctx);
```

Available accessors:

| Method | Description |
|------|------|
| `GetSessionId()` / `GetUserId()` | Built-in fields used to route the state slot and tenant |
| `GetAgentState()` / `SetAgentState(AgentState)` | Call-scoped `AgentState`, injected by the framework at call entry. Middleware and tools should read state from here, not from `agent.GetAgentState()` |
| `ResolveAgentState(ctx, agent)` | Static helper: returns `ctx.GetAgentState()` if available, falls back to `agent.GetAgentState()`. Use this in middleware/tools for concurrency safety |
| `Get(string)` / `Put(string, object)` | String-keyed get/put |
| `Get<T>()` / `Put<T>(T)` | Typed singleton get/put |
| `GetExtra()` | Direct access to the string-attribute map (mutable view) |
| `RuntimeContext.Empty` | Empty context |

:::{tip}
**The `IAgentStateStore` is bound at builder time and cannot be switched per call via `RuntimeContext`.** What *does* vary per call is the `(UserId, SessionId)` slot it addresses — set `UserId` for per-user isolation (or a custom `KeyPrefix` on the store); do not try to hand each call a different state store instance.
:::

:::{tip}
**Accessing `AgentState` from middleware and tools:** Always use `RuntimeContext.ResolveAgentState(ctx, agent)` rather than `agent.GetAgentState()` during call execution. Under concurrency, `agent.GetAgentState()` returns the last-active session's state (an arbitrary choice when multiple calls are in flight), while `ctx.GetAgentState()` returns the state for **this call's** session — which is what you almost always want.
:::

---

## Related pages

- [Agent](./agent.md) — full `ReActAgent` API and builder fields
- [Context Compaction](../harness/compaction.md) — conversation summarization, tool-result eviction, overflow recovery (builds on top of the AgentState foundation described here)
- [Memory](../harness/memory.md) — long-term memory, background maintenance
- [Permissions](./permission-system.md) — persistence of permission rules
