# A2A (Agent-to-Agent)

`AgentScope.Core.A2A` implements the [A2A protocol](https://a2aproject.github.io/A2A/) with client and server components.

## Client: calling a remote A2A Agent

### A2aAgent

`A2aAgent` (`AgentScope.Core.A2A.Client`) extends `AgentBase` to wrap a remote A2A Agent as a local Agent.

```csharp
using AgentScope.Core.A2A.Client;
using AgentScope.Core.A2A.Client.Card;
using AgentScope.Core.Service.Discovery;

// Resolve the remote AgentCard
var card = new AgentCard("remote-1", "translator", "Translation service", "http://other-service:8080");
var resolver = new FixedAgentCardResolver(card);
var agent = new A2aAgent("translator", resolver);

// Call like any local Agent
var result = await agent.CallAsync(new Msg[] { Msg.Builder().Role("user").TextContent("Hello").Build() });
```

### IAgentCardResolver Implementations

| Implementation | Description |
| --- | --- |
| `FixedAgentCardResolver(AgentCard card)` | Always returns the same AgentCard |
| `WellKnownAgentCardResolver(HttpClient http)` | Fetches the card from `https://{agentName}/.well-known/agent-card.json` |

### Interface

```csharp
public interface IAgentCardResolver
{
    Task<AgentCard?> ResolveAsync(string agentName, CancellationToken ct = default);
}
```

## Server: exposing a local Agent as an A2A Server

### AgentScopeA2aServer

```csharp
using AgentScope.Core.A2A.Server;
using AgentScope.Core.A2A.Server.Card;
using AgentScope.Core.A2A.Server.Executor.Runner;

// Create a runner that builds a new Agent per request
var runner = new ReActAgentWithBuilderRunner(
    agentFactory: () => new ReActAgent("backend-agent", model),
    name: "backend-agent",
    description: "Backend service Agent");

// Build the A2A Server
var server = new AgentScopeA2aServer(runner, new ConfigurableAgentCard
{
    Name = "backend-agent",
    Description = "Backend service Agent",
    Url = "http://localhost:5000"
});

// Register with external registry (e.g. Nacos)
server.AddRegistry(nacosRegistry);

// Handle requests from your web framework
var response = await server.HandleRequestAsync(requestBody, headers);

// Trigger registration after the web server is ready
await server.PostEndpointReadyAsync();
```

### Key Components

| Class | Namespace | Description |
| --- | --- | --- |
| `AgentScopeA2aServer` | `AgentScope.Core.A2A.Server` | Server entry point — assembles components and handles requests |
| `ConfigurableAgentCard` | `AgentScope.Core.A2A.Server.Card` | Configurable AgentCard builder; call `Build()` to produce `AgentCard` |
| `AgentScopeAgentCardConverter` | `AgentScope.Core.A2A.Server.Card` | Converts `ConfigurableAgentCard` to `AgentCard` |
| `AgentScopeAgentExecutor` | `AgentScope.Core.A2A.Server.Executor` | Executor with blocking `ExecuteAsync` and streaming `StreamAsync` |
| `ReActAgentWithBuilderRunner` | `AgentScope.Core.A2A.Server.Executor.Runner` | Default runner creating a new Agent per request |
| `IAgentRunner` | `AgentScope.Core.A2A.Server.Executor.Runner` | Runner interface with `StreamAsync` and `StopAsync` |

> `AgentScopeA2aServer` does not bind a port. It only builds components and the request-handling chain — HTTP serving is handled by your web framework.
