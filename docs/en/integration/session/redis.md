# Redis Session State

Persist agent session state in Redis using the `AgentScope.Extensions.Store.Redis` package (powered by StackExchange.Redis 3.x).

## Dependency

```xml
<ItemGroup>
  <PackageReference Include="AgentScope.Extensions.Store.Redis" Version="2.0.1" />
</ItemGroup>
```

Target framework: net10.0.

## Quick Start

### Direct Construction

```csharp
using AgentScope.Core;
using AgentScope.Core.Memory;
using AgentScope.Core.Model;
using AgentScope.Core.State;
using AgentScope.Extensions.Store.Redis;

// Convenience constructor: creates the underlying RedisDistributedStore automatically
var stateStore = new RedisAgentStateStore("redis://localhost:6379");

var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);

EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Name("assistant")
    .Model(new DashScopeModel("qwen-plus", apiKey))
    .Memory(memory)
    .Build();

await agent.CallAsync(Msg.Builder().Role("user").TextContent("Hello").Build());
```

### Explicit DistributedStore

```csharp
using AgentScope.Extensions.Store.Redis;

var redisStore = new RedisDistributedStore("redis://localhost:6379");
var stateStore = new RedisAgentStateStore(redisStore, keyPrefix: "agentstate");
```

## Custom Key Prefix

Use a key prefix to isolate data when multiple environments share a Redis instance:

```csharp
var stateStore = new RedisAgentStateStore(
    new RedisDistributedStore("redis://localhost:6379"),
    keyPrefix: "myapp:state");
```

The default `keyPrefix` is `"agentstate"`.

## Save and Restore Session

```csharp
using AgentScope.Core.Session;

var sessionManager = new SessionManager();
Session session = sessionManager.CreateSession(name: "redis-demo");

// Save
agent.SaveTo(session, "main");

// Restore
agent.LoadIfExists(session, "main");
```

## Versioning and Optimistic Concurrency

`RedisAgentStateStore` supports `SupportsVersioning`:
- `GetVersionedAsync` — Retrieves state together with its current version number
- `SaveIfVersionAsync` — Writes only when the version matches; no-op otherwise

This is critical for conflict detection in multi-replica deployments.

## Failover

- StackExchange.Redis has built-in connection multiplexing and auto-reconnect.
- Use Redis Sentinel or Redis Cluster for high availability in production.
- Connection string example: `redis://password@host:6379?ssl=true`.
