# MySQL Session State

Persist agent session state in MySQL using the `AgentScope.Extensions.Store.MySql` package (powered by MySqlConnector 2.x).

## Dependency

```xml
<ItemGroup>
  <PackageReference Include="AgentScope.Extensions.Store.MySql" Version="2.0.1" />
</ItemGroup>
```

Target framework: net10.0.

## Quick Start

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

await agent.CallAsync(Msg.Builder().Role("user").TextContent("Hello").Build());
```

## Custom Key Prefix

```csharp
var stateStore = new MySqlAgentStateStore(
    new MySqlDistributedStore(connectionString),
    keyPrefix: "myapp");
```

The default `keyPrefix` is `"agentstate"`.

## Save and Restore Session

```csharp
using AgentScope.Core.Session;

var sessionManager = new SessionManager();
Session session = sessionManager.CreateSession(name: "mysql-demo");

agent.SaveTo(session, "main");
agent.LoadIfExists(session, "main");
```

## Versioning Support

`MySqlAgentStateStore` supports `SupportsVersioning` with row-level optimistic concurrency. See [Distributed Storage — MySQL](../distributed/mysql.md).

## Production Considerations

- Use a connection pool for database connections.
- Set a descriptive `keyPrefix` to share the database across multiple applications.
- MySQL is ideal when you already have a relational database infrastructure and need SQL auditing capabilities.
