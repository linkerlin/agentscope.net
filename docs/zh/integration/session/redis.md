```{note}
本页面内容已迁移至 [分布式存储 — Redis](../distributed/redis.md)。以下内容保留作为参考，但建议使用新文档。
```

# Redis 状态存储

`agentscope-extensions-redis` 把 AgentScope 的 Agent 状态存到 Redis。统一抽象出 `RedisClientAdapter`，支持 **Jedis、Lettuce、Redisson** 三个客户端，覆盖 Standalone、Cluster、Sentinel 等部署模式。

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Redis" Version="$(AgentScopeVersion)" />
</ItemGroup>
```

模块本身不强制依赖某一客户端，按你项目里实际用的引入即可（Jedis / Lettuce / Redisson）。

## 快速上手（Lettuce 单机）

```csharp
using io.lettuce.core.RedisClient;
using AgentScope.core.state.AgentStateStore;
using AgentScope.extensions.redis.state.RedisAgentStateStore;
RedisClient redisClient = RedisClient.create("redis://localhost:6379");
AgentStateStore stateStore = RedisAgentStateStore.Builder()
    .LettuceClient(redisClient)
    .Build();
```

## 三种客户端的接入

### Jedis

```csharp
using redis.clients.jedis.UnifiedJedis;
UnifiedJedis jedis = new redis.clients.jedis.JedisPooled("localhost", 6379);
AgentStateStore stateStore = RedisAgentStateStore.Builder()
    .JedisClient(jedis)   // UnifiedJedis、JedisCluster、JedisSentineled 都可
    .Build();
```

### Lettuce 集群

```csharp
using io.lettuce.core.cluster.RedisClusterClient;
using io.lettuce.core.RedisURI;
RedisClusterClient clusterClient = RedisClusterClient.create(
    RedisURI.create("redis://localhost:7000"));
AgentStateStore stateStore = RedisAgentStateStore.Builder()
    .LettuceClusterClient(clusterClient)
    .Build();
```

### Redisson

```csharp
using org.redisson.Redisson;
using org.redisson.config.Config;
Config config = new Config();
config.UseSingleServer().SetAddress("redis://localhost:6379");
RedissonClient redisson = Redisson.create(config);
AgentStateStore stateStore = RedisAgentStateStore.Builder()
    .RedissonClient(redisson)
    .Build();
```

> Redisson 还支持 `useClusterServers()` / `useSentinelServers()` / `useMasterSlaveServers()`，配好后照样传给 `redissonClient(...)`。

## 自定义 key 前缀

默认所有 key 形如 `agentscope:session:{userSegment}/{sessionId}:...`（`userSegment` 即 `userId`，匿名 session 用 `__anon__`）。多个项目共享同一个 Redis 时建议自定义：

```csharp
AgentStateStore stateStore = RedisAgentStateStore.Builder()
    .LettuceClient(redisClient)
    .KeyPrefix("myapp:session:")
    .Build();
```

## Key 结构

`(userId, sessionId)` 二元组会被打包成单一槽位标识 `{userSegment}/{sessionId}`（`userSegment` 为 `userId`，`userId` 为 null 时取 `__anon__`）。

| 类型 | Key 模式 |
| --- | --- |
| 单值 | `{prefix}{userSegment}/{sessionId}:{stateKey}`（Redis String，存 JSON） |
| 列表 | `{prefix}{userSegment}/{sessionId}:{stateKey}:list`（Redis List，每项一条 JSON） |
| 列表 hash | `{prefix}{userSegment}/{sessionId}:{stateKey}:list:_hash`（变更检测用） |
| Session 索引 | `{prefix}{userSegment}/{sessionId}:_keys`（Redis Set，记录该 session 下所有 stateKey） |

`_keys` 索引让 `delete(userId, sessionId)`、`exists(userId, sessionId)` 都只需要常数次 Redis 调用，避免 `KEYS *`。

## 挂载到 Agent

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model(model)
    .StateStore(stateStore)
    .Build();
```

之后你的 Memory、Workspace、Plan 等组件就会自动通过 Redis 持久化。每次调用读写哪个槽位，由该次调用的 `RuntimeContext` 决定：

```csharp
RuntimeContext rc = RuntimeContext.Builder()
    .UserId("alice")
    .SessionId("session-1")
    .Build();
agent.Call(msg, rc).GetAwaiter().GetResult();
```

## 自定义客户端适配

如果你需要接其他 Redis 兼容存储（比如 KeyDB、Tair），可以实现 `RedisClientAdapter` 然后通过 `clientAdapter(...)` 注入：

```csharp
AgentStateStore stateStore = RedisAgentStateStore.Builder()
    .ClientAdapter(new MyCustomAdapter(...))
    .Build();
```

## Builder 配置参数

| 方法 | 说明 |
| --- | --- |
| `jedisClient(UnifiedJedis)` | Jedis 单机/集群/哨兵任意一种 |
| `lettuceClient(RedisClient)` | Lettuce 单机/Sentinel |
| `lettuceClusterClient(RedisClusterClient)` | Lettuce 集群 |
| `redissonClient(RedissonClient)` | Redisson 任意部署模式 |
| `clientAdapter(RedisClientAdapter)` | 自定义适配器 |
| `keyPrefix(String)` | 默认 `agentscope:session:` |

> 上述客户端方法互斥，只设置一个。
