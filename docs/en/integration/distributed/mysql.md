# MySQL / JDBC

`AgentScope.Extensions.MySql` provides full-stack JDBC-based distributed storage for teams with existing relational database infrastructure.

## Dependency

```xml
<PackageReference Include="AgentScope.Extensions.MySql" Version="$(AgentScopeVersion)" />
```

Add your database driver separately (e.g. `MySqlConnector`, `Npgsql`).

## One-Line Setup

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

## Components Provided

### 1. MysqlAgentStateStore

Agent state persisted to a MySQL table.

```csharp
using AgentScope.Extensions.MySql.State;

// auto-create schema
AgentStateStore store = new MysqlAgentStateStore(dataSource, true);

// custom DB/table names
AgentStateStore store = new MysqlAgentStateStore(
    dataSource, "agentscope_prod", "session_state", true);
```

### 2. JdbcStore (BaseStore)

Workspace filesystem KV storage with auto-detected dialect.

```csharp
using AgentScope.Extensions.MySql.Store;

BaseStore store = JdbcStore.Builder(dataSource)
    .InitializeSchema(true)
    .Build();
```

Supported dialects (auto-detected): MySQL, PostgreSQL, H2, SQLite.

### 3. JdbcSnapshotSpec

Sandbox snapshots as LONGBLOB in a database table.

```csharp
using AgentScope.Extensions.MySql.Snapshot;

SandboxSnapshotSpec spec = new JdbcSnapshotSpec(dataSource);
```

### 4. JdbcSandboxExecutionGuard

Distributed lock via MySQL `GET_LOCK()` / `RELEASE_LOCK()`.

```csharp
using AgentScope.Extensions.MySql.Sandbox;

SandboxExecutionGuard guard = JdbcSandboxExecutionGuard.Builder(dataSource)
    .KeyPrefix("myapp:lock:")
    .LockTimeout(TimeSpan.FromMinutes(30))
    .Build();
```

Lock is tied to the JDBC connection — auto-released on connection close.

## When to Use

| Scenario | Recommendation |
|----------|---------------|
| Existing MySQL, don't want Redis | **First choice**: MySQL |
| Need SQL audit / reporting / joins | MySQL |
| Large snapshots (>100MB) | MySQL BLOB works but consider OSS |
| Lowest latency | Redis |
