# Redis Distributed Storage

`AgentScope.Extensions.Store.Redis` provides Redis-based distributed state storage powered by StackExchange.Redis 3.x.

## Dependency

```xml
<ItemGroup>
  <PackageReference Include="AgentScope.Extensions.Store.Redis" Version="2.0.1" />
</ItemGroup>
```

Target framework: net10.0.

## RedisDistributedStore

Low-level distributed store implementing `IDistributedStore`:

```csharp
using AgentScope.Extensions.Store.Redis;

var store = new RedisDistributedStore("redis://localhost:6379");
```

The connection string supports all StackExchange.Redis configuration options, including password, SSL, and timeouts:

```
redis://password@host:6379?ssl=true&connectTimeout=5000
```

## RedisAgentStateStore

```csharp
using AgentScope.Extensions.Store.Redis;

// Option 1: Direct construction from connection string
var stateStore = new RedisAgentStateStore("redis://localhost:6379");

// Option 2: Via RedisDistributedStore
var redisStore = new RedisDistributedStore("redis://localhost:6379");
var stateStore = new RedisAgentStateStore(redisStore);

// Option 3: Custom key prefix
var stateStore = new RedisAgentStateStore(redisStore, keyPrefix: "prod:state");
```

The default `keyPrefix` is `"agentstate"`.

## Integration with StateBackedMemory

```csharp
using AgentScope.Core;
using AgentScope.Core.Memory;
using AgentScope.Core.State;
using AgentScope.Extensions.Store.Redis;

var stateStore = new RedisAgentStateStore("redis://localhost:6379");
var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);
```

## Versioning Support

`RedisAgentStateStore.SupportsVersioning = true`. Atomic CAS is implemented using Redis transactions or Lua scripts, suitable for multi-replica conflict detection.

## Production Considerations

- Use Redis Sentinel or Redis Cluster for high availability.
- Isolate environments and applications via `keyPrefix`.
- Monitor Redis memory usage; configure `maxmemory` and an eviction policy.
- StackExchange.Redis handles connection multiplexing and auto-reconnect automatically.

## Related Documentation

- [Session State — Redis](../session/redis.md) — RedisAgentStateStore + Session usage examples
- [Distributed Storage Overview](index.md) — Backend comparison and selection guide
