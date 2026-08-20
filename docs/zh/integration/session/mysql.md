# MySQL 会话状态

使用 `AgentScope.Extensions.Store.MySql` 包（基于 MySqlConnector 2.x）将 Agent 会话状态持久化到 MySQL。

## 安装

```xml
<ItemGroup>
  <PackageReference Include="AgentScope.Extensions.Store.MySql" Version="2.0.1" />
</ItemGroup>
```

目标框架：net10.0。

## 快速开始

```csharp
using AgentScope.Core;
using AgentScope.Core.Memory;
using AgentScope.Core.Model;
using AgentScope.Core.State;
using AgentScope.Extensions.Store.MySql;

var mysqlStore = new MySqlDistributedStore(
    "Server=localhost;Database=agentscope;User=root;Password=***;");

var stateStore = new MySqlAgentStateStore(mysqlStore, keyPrefix: "agentstate");

var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);

EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Name("assistant")
    .Model(new DashScopeModel("qwen-plus", apiKey))
    .Memory(memory)
    .Build();

await agent.CallAsync(Msg.Builder().Role("user").TextContent("你好").Build());
```

## 自定义 key 前缀

```csharp
var stateStore = new MySqlAgentStateStore(
    new MySqlDistributedStore(connectionString),
    keyPrefix: "myapp");
```

默认 `keyPrefix` 为 `"agentstate"`。

## 会话保存与恢复

```csharp
using AgentScope.Core.Session;

var sessionManager = new SessionManager();
Session session = sessionManager.CreateSession(name: "mysql-demo");

agent.SaveTo(session, "main");
agent.LoadIfExists(session, "main");
```

## 版本化支持

`MySqlAgentStateStore` 支持 `SupportsVersioning`，提供基于版本号的乐观并发控制。详见[分布式存储 — MySQL](../distributed/mysql.md)。

## 生产建议

- 使用连接池管理数据库连接。
- 建议将 `keyPrefix` 设为业务标识，便于多应用共享数据库实例。
- MySQL 适合已有关系型数据库基础设施、需要 SQL 审计的场景。
