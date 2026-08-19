# Redis

`AgentScope.Extensions.Redis` 提供全链路的 Redis 分布式存储实现，是多副本生产部署的首选后端。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Redis" Version="$(AgentScopeVersion)" />
```

模块本身不强制依赖某一 Redis 客户端，按项目实际使用引入（如 StackExchange.Redis）。

## 一键配置

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

自定义 key 前缀（多环境隔离）：

```csharp
DistributedStore store = RedisDistributedStore.FromConnectionMultiplexer(redis, "prod:");
```

## 提供的组件

### 1. RedisAgentStateStore

Agent 状态持久化到 Redis。

```csharp
using AgentScope.Extensions.Redis.State;

AgentStateStore store = RedisAgentStateStore.Builder()
    .ConnectionMultiplexer(redis)
    .KeyPrefix("myapp:session:")
    .Build();
```

### 2. RedisStore（BaseStore）

工作区文件系统 KV 存储，供 `RemoteFilesystemSpec` 使用。

```csharp
using AgentScope.Extensions.Redis.Store;

BaseStore store = new RedisStore(redis);
BaseStore store = new RedisStore(redis, "myapp:store:");
```

**并发安全**：`Put` / `PutIfVersion` 使用 Lua 脚本保证原子性。

### 3. RedisSnapshotSpec

沙箱快照存储到 Redis 二进制 key。适合小工作区 + 短 TTL 场景。

```csharp
using AgentScope.Extensions.Redis.Snapshot;

SandboxSnapshotSpec spec = new RedisSnapshotSpec(redis, "myapp:snapshot:", 3600);
```

### 4. RedisSandboxExecutionGuard

基于 Redis `SET NX PX` 租约的分布式锁，用于 `AGENT` / `GLOBAL` 隔离范围下的多副本并发控制。

```csharp
using AgentScope.Extensions.Redis.Sandbox;

SandboxExecutionGuard guard = RedisSandboxExecutionGuard.Builder(redis)
    .KeyPrefix("myapp:guard:")
    .LeaseTtl(TimeSpan.FromMinutes(30))
    .RetryInterval(TimeSpan.FromMilliseconds(500))
    .Build();
```

## 选型建议

| 场景 | 建议 |
|------|------|
| 多副本生产，追求低延迟 | **首选** Redis |
| 已有 Redis 集群 | StackExchange.Redis |
| 小工作区 + 短 TTL 快照 | Redis 快照可以，但注意内存 |
| 大工作区快照 | 混合后端：Redis 管状态和锁，OSS 管快照 |
