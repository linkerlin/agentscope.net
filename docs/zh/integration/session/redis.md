# Redis 会话状态

使用 `AgentScope.Extensions.Store.Redis` 包（基于 StackExchange.Redis 3.x）将 Agent 会话状态持久化到 Redis。

## 安装

```xml
<ItemGroup>
  <PackageReference Include="AgentScope.Extensions.Store.Redis" Version="2.0.1" />
</ItemGroup>
```

目标框架：net10.0。

## 快速开始

### 方式一：直接构造

```csharp
using AgentScope.Core;
using AgentScope.Core.Memory;
using AgentScope.Core.Model;
using AgentScope.Core.State;
using AgentScope.Extensions.Store.Redis;

// 便捷构造：自动创建 RedisDistributedStore
var stateStore = new RedisAgentStateStore("redis://localhost:6379");

var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);

EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Name("assistant")
    .Model(new DashScopeModel("qwen-plus", apiKey))
    .Memory(memory)
    .Build();

await agent.CallAsync(Msg.Builder().Role("user").TextContent("你好").Build());
```

### 方式二：显式创建 DistributedStore

```csharp
using AgentScope.Extensions.Store.Redis;

var redisStore = new RedisDistributedStore("redis://localhost:6379");
var stateStore = new RedisAgentStateStore(redisStore, keyPrefix: "agentstate");
```

## 自定义 key 前缀

多个环境共享 Redis 实例时，通过 key 前缀隔离：

```csharp
var stateStore = new RedisAgentStateStore(
    new RedisDistributedStore("redis://localhost:6379"),
    keyPrefix: "myapp:state");
```

默认 `keyPrefix` 为 `"agentstate"`。

## 会话保存与恢复

```csharp
using AgentScope.Core.Session;

var sessionManager = new SessionManager();
Session session = sessionManager.CreateSession(name: "redis-demo");

// 保存
agent.SaveTo(session, "main");

// 恢复
agent.LoadIfExists(session, "main");
```

## 版本化与乐观并发

`RedisAgentStateStore` 支持 `SupportsVersioning`：
- `GetVersionedAsync` — 获取状态及其当前版本号
- `SaveIfVersionAsync` — 仅在版本号匹配时写入，否则不执行

这对多副本场景下的状态冲突检测至关重要。

## 故障转移

- StackExchange.Redis 内置连接复用与自动重连。
- 生产环境建议使用 Redis Sentinel 或 Redis Cluster 实现高可用。
- 连接串示例：`redis://password@host:6379?ssl=true`。
