# MySQL 分布式存储

`AgentScope.Extensions.Store.MySql` 基于 MySqlConnector 2.x 提供 MySQL 分布式状态存储实现。

## 安装

```xml
<ItemGroup>
  <PackageReference Include="AgentScope.Extensions.Store.MySql" Version="2.0.1" />
</ItemGroup>
```

目标框架：net10.0。

## MySqlDistributedStore

底层分布式存储，实现 `IDistributedStore` 接口：

```csharp
using AgentScope.Extensions.Store.MySql;

var store = new MySqlDistributedStore(
    "Server=localhost;Database=agentscope;User=root;Password=***;");
```

连接串为标准 MySQL 连接串格式，支持 MySqlConnector 所有选项：

```
Server=host;Port=3306;Database=agentscope;User=root;Password=***;SslMode=Preferred;
```

## MySqlAgentStateStore

```csharp
using AgentScope.Extensions.Store.MySql;

var mysqlStore = new MySqlDistributedStore(connectionString);
var stateStore = new MySqlAgentStateStore(mysqlStore);

// 自定义 key 前缀
var stateStore = new MySqlAgentStateStore(mysqlStore, keyPrefix: "prod:state");
```

默认 `keyPrefix` 为 `"agentstate"`。

## 与 StateBackedMemory 集成

```csharp
using AgentScope.Core;
using AgentScope.Core.Memory;
using AgentScope.Core.State;
using AgentScope.Extensions.Store.MySql;

var stateStore = new MySqlAgentStateStore(
    new MySqlDistributedStore(connectionString));
var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);
```

## 版本化支持

`MySqlAgentStateStore.SupportsVersioning = true`。基于 MySQL 行级锁或乐观锁实现 CAS。

## 生产建议

- 使用连接池管理 MySQL 连接。
- 为 `agentscope` 数据库配置定期备份。
- 监控慢查询，确保状态读写性能。
- 多副本部署时启用版本化 CAS 防止状态冲突。

## 相关文档

- [会话状态 — MySQL](../session/mysql.md) — MySqlAgentStateStore + Session 使用示例
- [分布式存储总览](index.md) — 后端对比与选型指南
