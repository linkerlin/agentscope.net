# Nacos

`AgentScope.Extensions.Nacos` uses [Nacos](https://nacos.io/) as AgentScope's unified control plane: register/discover A2A Agents, hot-load prompts, and host skills. Three sub-modules — pick what you need.

| Sub-module | Problem it solves |
| --- | --- |
| `AgentScope.Extensions.Nacos` (core) | A2A AgentCard registry & discovery, implements `IAgentRegistry` |
| `AgentScope.Extensions.Nacos.Prompt` | Manage prompt templates in Nacos with runtime hot-update |
| `AgentScope.Extensions.Nacos.Skill` | Load skill packages from Nacos |

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Nacos" Version="2.0.1" />
```

## A2A Registry & Discovery

### NacosAgentRegistry (server-side)

`NacosAgentRegistry` implements `AgentScope.Core.Service.Discovery.IAgentRegistry` via the Nacos HTTP Open API.

```csharp
using AgentScope.Extensions.Nacos;
using AgentScope.Core.Service.Discovery;

var registry = new NacosAgentRegistry(
    httpClient,
    serverAddr: "http://localhost:8848",
    groupName: "DEFAULT_GROUP");

// Register an AgentCard
await registry.RegisterAsync(new AgentCard(
    "agent-1", "translator", "Translation Agent", "192.168.1.1:8080"));

// Resolve by name
var card = await registry.ResolveAsync("translator");

// List all registered Agents
await foreach (var c in registry.ListAsync()) { ... }

// Unregister
await registry.UnregisterAsync("translator");
```

### IAgentRegistry Interface

| Method | Description |
| --- | --- |
| `ValueTask RegisterAsync(AgentCard card, CancellationToken ct)` | Register as ephemeral Nacos instance |
| `ValueTask UnregisterAsync(string agentId, CancellationToken ct)` | Delete instance from Nacos |
| `ValueTask<AgentCard?> ResolveAsync(string agentId, CancellationToken ct)` | Query healthy instances and build AgentCard |
| `IAsyncEnumerable<AgentCard> ListAsync(CancellationToken ct)` | List all registered AgentCards |

### NacosAgentCardResolver (client-side)

```csharp
var resolver = new NacosAgentCardResolver(
    httpClient,
    serverAddr: "http://localhost:8848",
    groupName: "DEFAULT_GROUP");

var card = await resolver.ResolveAsync("translator");
```

### Configuration

```csharp
var options = new NacosA2aRegistryOptions
{
    ServerAddr = "http://localhost:8848",
    Namespace = "",
    GroupName = "DEFAULT_GROUP",
    HeartbeatInterval = TimeSpan.FromSeconds(5)
};
```

## Prompt Config Center

```csharp
using AgentScope.Extensions.Nacos.Prompt;

var repo = new NacosPromptRepository(
    serverAddr: "http://localhost:8848",
    namespaceId: null,
    group: null,
    http: httpClient);
```

`NacosPromptRepository` reads prompt templates from the Nacos config center with runtime hot-update support.

## Skill Repository

```csharp
using AgentScope.Extensions.Nacos.Skill;

var repo = new NacosSkillRepository(
    serverAddr: "http://localhost:8848",
    namespaceId: null,
    group: null,
    http: httpClient);
```

`NacosSkillRepository` loads skill packages from Nacos, providing `GetSkillContentAsync` and `PublishSkillAsync` methods (standalone API, does not implement `ISkillRepository`).

## Pairs well with

- [A2A](../protocol/a2a.md): call `AgentScopeA2aServer.AddRegistry(registry)` to publish AgentCards to Nacos on startup.
- [A2A client](../protocol/a2a.md): use `NacosAgentCardResolver` as the `IAgentCardResolver` for `A2aAgent`.
