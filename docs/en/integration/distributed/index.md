# Distributed Storage (Distributed Store)

AgentScope unifies all components that need distributed persistence under the `DistributedStore` interface. One line of configuration switches agent state, workspace filesystem, sandbox snapshots, and concurrency locks to the same distributed store.

## Quick Start

```csharp
// Redis — one-line setup
DistributedStore store = RedisDistributedStore.FromJedis(
        new JedisPooled("redis://localhost:6379"));

HarnessAgent agent = HarnessAgent.Builder()
    .Name("my-agent")
    .Model("dashscope:qwen-plus")
    .DistributedStore(store)
    .Filesystem(new RemoteFilesystemSpec()            // baseStore auto-injected
            .IsolationScope(IsolationScope.USER))
    .Build();
```

## Capability Matrix

| Component | Interface | Redis | OSS | MySQL |
|-----------|----------|:-----:|:---:|:-----:|
| Agent state persistence | `AgentStateStore` | `RedisAgentStateStore` | `OssAgentStateStore` | `MysqlAgentStateStore` |
| Workspace filesystem KV | `BaseStore` | `RedisStore` | `OssBaseStore` | `JdbcStore` |
| Sandbox snapshots | `SandboxSnapshotSpec` | `RedisSnapshotSpec` | `OssSnapshotSpec` | `JdbcSnapshotSpec` |
| Sandbox concurrency lock | `SandboxExecutionGuard` | `RedisSandboxExecutionGuard` | — | `JdbcSandboxExecutionGuard` |

> OSS does not provide `SandboxExecutionGuard` — object storage is unsuitable for distributed locking. Mix in a Redis guard via `DistributedStore.Builder()`.

## Mixed Stores

Different components can come from different storage stores:

```csharp
DistributedStore mysql = MysqlDistributedStore.Create(dataSource);
DistributedStore redis = RedisDistributedStore.FromJedis(jedis);

// MySQL for state and files, Redis for sandbox lock and snapshots
DistributedStore mixed = DistributedStore.Builder()
    .AgentStateStore(mysql.AgentStateStore())
    .BaseStore(mysql.BaseStore())
    .SandboxSnapshotSpec(redis.SandboxSnapshotSpec())
    .SandboxExecutionGuard(redis.SandboxExecutionGuard())
    .Build();

HarnessAgent.Builder()
    .DistributedStore(mixed)
    .Filesystem(new DockerFilesystemSpec()
            .Image("ubuntu:24.04"))
    .Build();
```

## Components

### AgentStateStore — Agent State Persistence

Conversation context, compaction summaries, permission rules, Plan Mode state, addressed by `(userId, sessionId)`. Auto-wired by `DistributedStore`; can be overridden via `.StateStore(...)`.

### BaseStore — Workspace Filesystem KV

Storage provider for `RemoteFilesystemSpec`, routing `MEMORY.md`, `memory/`, `skills/`, `sessions/` to shared KV storage. Auto-injected into `RemoteFilesystemSpec` when using the no-arg constructor.

### SandboxSnapshotSpec — Sandbox Snapshots

Persists Docker/K8s sandbox workspace as tar archives for cross-call recovery. Auto-wired into `SandboxFilesystemSpec` by `DistributedStore`.

### SandboxExecutionGuard — Sandbox Concurrency Lock

Distributed lock for `AGENT` / `GLOBAL` isolation scope under multi-replica deployment. Auto-wired into `SandboxFilesystemSpec` by `DistributedStore`.

## Priority

```
Explicit builder methods (.StateStore(), .SnapshotSpec() on FilesystemSpec, etc.)
    > DistributedStore auto-wiring
        > local defaults (JsonFileAgentStateStore, NoopSnapshotSpec, etc.)
```

## Store Documentation

- [Redis](redis.md) — full capability coverage, recommended for multi-replica production
- [MySQL / JDBC](mysql.md) — for existing relational database infrastructure
- [Alibaba Cloud OSS](oss.md) — object storage, best for large-capacity snapshots

## aistio Hosted Store

When you already run an aistio control plane, it can host the coordination side of `DistributedStore` (BaseStore, sandbox lock/snapshot, MessageBus, AsyncToolRegistry, **TaskRepository**, optional **SessionTurnGate**). You still provide **one** `AgentStateStore` backend yourself (Redis / MySQL / Postgres / OSS); core exposes `GetVersioned` / `SaveIfVersion` optimistic concurrency, but state storage stays off the control plane.

```csharp
ControlPlaneStores cp = ControlPlaneStores.FromEnv();
HarnessAgent.Builder()
    .DistributedStore(cp.WithAgentStateStore(redis.AgentStateStore()))
    .Filesystem(new RemoteFilesystemSpec().IsolationScope(IsolationScope.USER))
    .Build();
```

- Enable on the control plane with `--enable-hosted-store` (Postgres recommended for production).
- **`WithAgentStateStore` includes** hosted `TaskRepository` and `SessionTurnGate`. With **`SandboxFilesystemSpec` and subagent background tasks**, use this path — the workspace `TaskRepository` cannot persist tasks across replicas.
- **AgentStateStore versioning**: Redis, Postgres, MySQL, and InMemory support CAS; JsonFile, OSS, COS, and JPA remain last-writer-wins. Prefer a versioning backend for multi-replica deployments.
- **Turn gate + `ConflictPolicy.FAIL`** are optional: they reduce duplicate LLM turns on multi-replica setups; correctness still comes from CAS when the backend supports versioning.
- Auth today is a shared internal token; tenant (`agentName` / `namespace`) comes from the request body — **not** for mutually untrusted multi-tenant agents on one control plane.
- `MessageBus.QueueDrain` is **destructive** (ack-on-read); a wrong tenant key drops messages.
