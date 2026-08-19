# MySQL / JDBC

`AgentScope.Extensions.MySql` 提供基于 JDBC 的全链路分布式存储实现，适合已有关系型数据库基础设施的场景。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.MySql" Version="$(AgentScopeVersion)" />
```

数据库驱动按实际使用的版本自行引入（如 `MySqlConnector`、`Npgsql`）。

## 一键配置

```csharp
using AgentScope.Extensions.MySql;

DataSource dataSource = ...;  // HikariCP, Druid, etc.
DistributedStore store = MysqlDistributedStore.Create(dataSource);

HarnessAgent agent = HarnessAgent.Builder()
    .DistributedStore(store)
    .Filesystem(new RemoteFilesystemSpec()
            .IsolationScope(IsolationScope.USER))
    .Build();
```

## 提供的组件

### 1. MysqlAgentStateStore

Agent 状态持久化到 MySQL 表。

```csharp
using AgentScope.Extensions.MySql.State;

// 自动建库建表
AgentStateStore store = new MysqlAgentStateStore(dataSource, true);

// 自定义库名 / 表名
AgentStateStore store = new MysqlAgentStateStore(
    dataSource, "agentscope_prod", "session_state", true);
```

### 2. JdbcStore（BaseStore）

工作区文件系统 KV 存储，支持多种数据库方言。

```csharp
using AgentScope.Extensions.MySql.Store;

BaseStore store = JdbcStore.Builder(dataSource)
    .InitializeSchema(true)
    .Build();
```

**支持的方言**（自动检测）：MySQL, PostgreSQL, H2, SQLite。

### 3. JdbcSnapshotSpec

沙箱快照以 LONGBLOB 存储到数据库表。

```csharp
using AgentScope.Extensions.MySql.Snapshot;

SandboxSnapshotSpec spec = new JdbcSnapshotSpec(dataSource);
```

### 4. JdbcSandboxExecutionGuard

基于 MySQL `GET_LOCK()` / `RELEASE_LOCK()` 的分布式锁。

```csharp
using AgentScope.Extensions.MySql.Sandbox;

SandboxExecutionGuard guard = JdbcSandboxExecutionGuard.Builder(dataSource)
    .KeyPrefix("myapp:lock:")
    .LockTimeout(TimeSpan.FromMinutes(30))
    .Build();
```

锁绑定 JDBC 连接——连接关闭时自动释放。

## 选型建议

| 场景 | 建议 |
|------|------|
| 已有 MySQL，不想引入 Redis | **首选** MySQL |
| 需要 SQL 审计 / 报表 / 联表查询 | MySQL |
| 快照数据量大（>100MB） | MySQL BLOB 可行但推荐 OSS |
| 追求最低延迟 | Redis |
