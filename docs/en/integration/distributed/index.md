# Distributed Storage Overview

AgentScope abstracts distributed state storage through the `IAgentStateStore` interface, providing a unified Get/Set/Delete contract with optional versioned optimistic concurrency.

## IAgentStateStore Interface

Defined in `AgentScope.Core.State`:

```csharp
public interface IAgentStateStore
{
    bool SupportsVersioning { get; }

    Task<AgentState?> GetAsync(string userId, string sessionId, string key);
    Task<VersionedState<AgentState>?> GetVersionedAsync(string userId, string sessionId, string key);
    Task SaveAsync(string userId, string sessionId, string key, AgentState state);
    Task<long> SaveIfVersionAsync(string userId, string sessionId, string key, AgentState state, long expectedVersion);
}
```

- `SupportsVersioning` — Indicates whether the backend supports versioned operations
- `GetVersionedAsync` — Retrieves the state together with its current version number
- `SaveIfVersionAsync` — CAS (Compare-And-Swap) write: succeeds only when `expectedVersion` matches; returns the new version number

## Backend Matrix

| Backend | Package | Construction | Versioning | Use Case |
|---------|---------|-------------|:----------:|----------|
| **Redis** | `AgentScope.Extensions.Store.Redis` | `RedisAgentStateStore(RedisDistributedStore)` / `RedisAgentStateStore(connectionString)` | ✅ | Multi-replica production, low latency |
| **MySQL** | `AgentScope.Extensions.Store.MySql` | `MySqlAgentStateStore(MySqlDistributedStore)` | ✅ | Existing MySQL infrastructure |
| **PostgreSQL** | `AgentScope.Extensions.Store.PostgreSql` | `PostgreSqlAgentStateStore(PostgreSqlDistributedStore)` | ✅ | PostgreSQL ecosystem |
| **OSS** | `AgentScope.Extensions.Store.Oss` | `OssAgentStateStore(OssDistributedStore)` | ❌ | Alibaba Cloud, large capacity |
| **COS** | `AgentScope.Extensions.Store.Cos` | `CosAgentStateStore(CosStore)` | ❌ | Tencent Cloud ecosystem |

## IDistributedStore Low-Level Interface

All `*DistributedStore` types implement `IDistributedStore`:

- `Get(string key)` — Fetches raw data
- `Set(string key, byte[] value)` — Stores raw data
- `Delete(string key)` — Deletes data
- `ListKeys(string prefix)` — Lists keys by prefix

## How to Choose

1. **Low latency, multi-replica** → **Redis** (versioning supported, production-first)
2. **Existing MySQL/PostgreSQL** → **MySQL/PostgreSQL** (versioning supported, shared database)
3. **Alibaba/Tencent Cloud ecosystem, large archives** → **OSS/COS** (no versioning, last-writer-wins)
4. **Local development / debugging** → `InMemoryAgentStateStore` or `JsonFileAgentStateStore`

## Integration with StateBackedMemory

Any `IAgentStateStore` can be used with `StateBackedMemory`:

```csharp
var stateStore = new RedisAgentStateStore("redis://localhost:6379");
var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);
```

## Detailed Documentation

- [Redis Backend](redis.md) — Connection string format, construction, production advice
- [MySQL Backend](mysql.md) — Connection string format, construction
- [OSS Backend](oss.md) — Alibaba Cloud OSS integration
- [Session State Integration](../session/index.md) — SessionManager and state persistence usage
