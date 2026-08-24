# MySQL Distributed Storage

`AgentScope.Extensions.Store.MySql` provides MySQL-based distributed state storage powered by MySqlConnector 2.x.

## Dependency

```xml
<ItemGroup>
  <PackageReference Include="AgentScope.Extensions.Store.MySql" Version="2.0.1" />
</ItemGroup>
```

Target framework: net10.0.

## MySqlDistributedStore

Low-level distributed store implementing `IDistributedStore`:

```csharp
using AgentScope.Extensions.Store.MySql;

var store = new MySqlDistributedStore(
    "Server=localhost;Database=agentscope;User=root;Password=***;");
```

The connection string follows the standard MySQL format with support for all MySqlConnector options:

```
Server=host;Port=3306;Database=agentscope;User=root;Password=***;SslMode=Preferred;
```

## MySqlAgentStateStore

```csharp
using AgentScope.Extensions.Store.MySql;

var mysqlStore = new MySqlDistributedStore(connectionString);
var stateStore = new MySqlAgentStateStore(mysqlStore);

// Custom key prefix
var stateStore = new MySqlAgentStateStore(mysqlStore, keyPrefix: "prod:state");
```

The default `keyPrefix` is `"agentstate"`.

## Integration with StateBackedMemory

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

## Versioning Support

`MySqlAgentStateStore.SupportsVersioning = true`. CAS is implemented using MySQL row-level locks or optimistic locking.

## Production Considerations

- Use a connection pool for MySQL connections.
- Schedule regular backups for the `agentscope` database.
- Monitor slow queries to ensure state read/write performance.
- Enable versioned CAS to prevent state conflicts in multi-replica deployments.

## Related Documentation

- [Session State — MySQL](../session/mysql.md) — MySqlAgentStateStore + Session usage examples
- [Distributed Storage Overview](index.md) — Backend comparison and selection guide
