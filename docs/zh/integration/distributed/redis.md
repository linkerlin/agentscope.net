# Redis 分布式存储

`AgentScope.Extensions.Store.Redis` 基于 StackExchange.Redis 3.x 提供 Redis 分布式状态存储实现。

## 安装

```xml
<ItemGroup>
  <PackageReference Include="AgentScope.Extensions.Store.Redis" Version="2.0.1" />
</ItemGroup>
```

目标框架：net10.0。

## RedisDistributedStore

底层分布式存储，实现 `IDistributedStore` 接口：

```csharp
using AgentScope.Extensions.Store.Redis;

var store = new RedisDistributedStore("redis://localhost:6379");
```

连接串支持 StackExchange.Redis 全部配置选项，包括密码、SSL、超时等：

```
redis://password@host:6379?ssl=true&connectTimeout=5000
```

## RedisAgentStateStore

```csharp
using AgentScope.Extensions.Store.Redis;

// 方式一：通过连接串直接构造
var stateStore = new RedisAgentStateStore("redis://localhost:6379");

// 方式二：通过 RedisDistributedStore 构造
var redisStore = new RedisDistributedStore("redis://localhost:6379");
var stateStore = new RedisAgentStateStore(redisStore);

// 方式三：自定义 key 前缀
var stateStore = new RedisAgentStateStore(redisStore, keyPrefix: "prod:state");
```

默认 `keyPrefix` 为 `"agentstate"`。

## 与 StateBackedMemory 集成

```csharp
using AgentScope.Core;
using AgentScope.Core.Memory;
using AgentScope.Core.State;
using AgentScope.Extensions.Store.Redis;

var stateStore = new RedisAgentStateStore("redis://localhost:6379");
var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);
```

## 版本化支持

`RedisAgentStateStore.SupportsVersioning = true`。使用 Redis 事务或 Lua 脚本实现原子 CAS，适用于多副本冲突检测。

## 生产建议

- 生产环境使用 Redis Sentinel 或 Redis Cluster 实现高可用。
- 通过 `keyPrefix` 隔离多环境/多应用的数据。
- 监控 Redis 内存用量，设置合理的 maxmemory 和淘汰策略。
- StackExchange.Redis 自动处理连接复用和重连，无需手动管理连接生命周期。

## 相关文档

- [会话状态 — Redis](../session/redis.md) — RedisAgentStateStore + Session 使用示例
- [分布式存储总览](index.md) — 后端对比与选型指南
