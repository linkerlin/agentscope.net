# Redis

`AgentScope.Extensions.Redis` provides full-stack Redis distributed storage — the recommended store for multi-replica production deployments.

## Dependency

```xml
<PackageReference Include="AgentScope.Extensions.Redis" Version="$(AgentScopeVersion)" />
```

The module does not force a specific Redis client — import whichever you use (StackExchange.Redis / Microsoft.Extensions.Caching.StackExchangeRedis).

## One-Line Setup

```csharp
using AgentScope.Extensions.Redis;

ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("redis://localhost:6379");
DistributedStore store = RedisDistributedStore.FromConnectionMultiplexer(redis);

HarnessAgent agent = HarnessAgent.Builder()
    .DistributedStore(store)
    .Filesystem(new RemoteFilesystemSpec()
            .IsolationScope(IsolationScope.USER))
    .Build();
```

Custom key prefix for multi-environment isolation:

```csharp
DistributedStore store = RedisDistributedStore.FromConnectionMultiplexer(redis, "prod:");
```

## Components Provided

### 1. RedisAgentStateStore

Agent state persisted to Redis.

```csharp
using AgentScope.Extensions.Redis.State;

AgentStateStore store = RedisAgentStateStore.Builder()
    .ConnectionMultiplexer(redis)
    .KeyPrefix("myapp:session:")
    .Build();
```

### 2. RedisStore (BaseStore)

Workspace filesystem KV storage for `RemoteFilesystemSpec`.

```csharp
using AgentScope.Extensions.Redis.Store;

BaseStore store = new RedisStore(redis);
BaseStore store = new RedisStore(redis, "myapp:store:");
```

Concurrency-safe: `Put` / `PutIfVersion` use Lua scripts for atomicity.

### 3. RedisSnapshotSpec

Sandbox snapshots stored as Redis binary keys. Best for small workspaces + short TTL.

```csharp
using AgentScope.Extensions.Redis.Snapshot;

SandboxSnapshotSpec spec = new RedisSnapshotSpec(redis, "myapp:snapshot:", 3600);
```

### 4. RedisSandboxExecutionGuard

Redis `SET NX PX` lease-based distributed lock for multi-replica sandbox concurrency control.

```csharp
using AgentScope.Extensions.Redis.Sandbox;

SandboxExecutionGuard guard = RedisSandboxExecutionGuard.Builder(redis)
    .KeyPrefix("myapp:guard:")
    .LeaseTtl(TimeSpan.FromMinutes(30))
    .RetryInterval(TimeSpan.FromMilliseconds(500))
    .Build();
```

## When to Use

| Scenario | Recommendation |
|----------|---------------|
| Multi-replica production, low latency | **First choice**: Redis |
| Existing Redis cluster | StackExchange.Redis |
| Small workspace + short TTL snapshots | Redis snapshots work, watch memory |
| Large workspace snapshots | Mixed store: Redis for state/lock, OSS for snapshots |
