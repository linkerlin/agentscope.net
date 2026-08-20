# Session State Management

AgentScope provides a complete session state management mechanism that supports agent runtime context persistence, recovery, and cross-session switching.

Core concepts:

- **`IAgentStateStore`** — State storage interface defined in `AgentScope.Core.State`. Supports `GetAsync`, `SaveAsync`, and optional versioned optimistic concurrency (`GetVersionedAsync`, `SaveIfVersionAsync`).
- **`StateBackedMemory`** — An `IMemory` wrapper that automatically persists every change to the associated `IAgentStateStore`.
- **`Session` / `SessionManager`** — Session lifecycle management: create, switch, pause, resume, and delete sessions.
- **`EnhancedReActAgent`** — Exposes `SaveTo(Session, sessionKey)` and `LoadFrom(Session, sessionKey)` methods for persisting agent state to a session.

## Documentation

- [Overview — Session & State Persistence](overview.md) — `Session`, `SessionManager`, `StateBackedMemory`, `EnhancedReActAgent` usage.
- [Redis Backend](redis.md) — Distributed session state with `RedisAgentStateStore`.
- [MySQL Backend](mysql.md) — Session persistence with `MySqlAgentStateStore`.
- [OSS Backend](oss.md) — Alibaba Cloud OSS integration via `OssAgentStateStore`.

For a combined setup covering agent state, workspace filesystem, sandbox snapshots, and concurrency locks, see [Distributed Store Overview](../distributed/index.md).
