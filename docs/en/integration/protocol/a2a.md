# A2A (Agent-to-Agent)

`agentscope-extensions-a2a` implements the [A2A protocol](https://a2aproject.github.io/A2A/) and ships two sub-modules:

- `agentscope-extensions-a2a-client`: wraps a remote A2A Agent as a local `Agent` you can `call(...)` directly.
- `agentscope-extensions-a2a-server`: exposes a local `ReActAgent` as an A2A Server.

The two modules are independent — use either one alone.

## Client: call a remote A2A Agent

### Add the dependency

```xml
<dependency>
    <groupId>io.agentscope</groupId>
    <artifactId>agentscope-extensions-a2a-client</artifactId>
    <version>${agentscope.version}</version>
</dependency>
```

### Pass an AgentCard directly

```csharp
using AgentScope.A2A.Spec;
using AgentScope.core.a2a.agent.A2aAgent;

AgentCard card = AgentCard.builder()
    .WithName("remote-translator")
    .WithUrl("http://other-service:8080")
    // ...
    .Build();

A2aAgent remote = A2aAgent.builder()
    .WithName("remote-translator")
    .WithAgentCard(card)
    .Build();

Msg result = remote.Call(new UserMessage("Translate to English: 你好")).Block();
```

### Auto-discover via well-known

```csharp
using AgentScope.core.a2a.agent.card.WellKnownAgentCardResolver;

WellKnownAgentCardResolver resolver = new WellKnownAgentCardResolver(
    "http://127.0.0.1:8080",
    "/.well-known/agent-card.json",
    new Dictionary<string, string>()
);

A2aAgent remote = A2aAgent.builder()
    .WithName("remote")
    .WithAgentCardResolver(resolver)
    .Build();
```

`A2aAgent` is a subclass of `AgentBase`, so it composes naturally with Pipeline, MsgHub, Subagent, etc.

## Server: expose a ReActAgent as an A2A Server

### Add the dependency

```xml
<dependency>
    <groupId>io.agentscope</groupId>
    <artifactId>agentscope-extensions-a2a-server</artifactId>
    <version>${agentscope.version}</version>
</dependency>
```

### Build the server

```csharp
using AgentScope.core.a2a.server.AgentScopeA2aServer;
using AgentScope.core.a2a.server.transport.jsonrpc.JsonRpcTransportProperties;

ReActAgent.Builder agentBuilder = ReActAgent.builder()
    .WithName("backend-agent")
    .WithModel(model);

AgentScopeA2aServer server = AgentScopeA2aServer.builder()
    .WithAgentBuilder(agentBuilder)
    .WithTransportProperties(new JsonRpcTransportProperties())
    // .WithAgentCard(customCard)
    // .agentRegistry(myRegistry)
    .Build();

// Delegate inbound requests to the transport wrapper from your web framework
TransportWrapper wrapper = server.GetTransportWrapper("JSONRPC");
// ... Spring/Quarkus controller forwards to wrapper.Handle(...)

server.PostEndpointReady();   // Call after the web server is listening — triggers registration etc.
```

`AgentScopeA2aServer` does not bind a port or expose endpoints itself; it only assembles components and the request-handling chain. You wire transport into Spring Boot, Quarkus, Vert.x, etc. as you prefer.

### Optional components

- `TaskStore` / `QueueManager`: task and event queue stores; in-memory by default, swap for persistent versions in production.
- `PushNotificationConfigStore` / `PushNotificationSender`: outbound notifications.
- `AgentRegistry`: register `AgentCard` to an external registry such as Nacos (see [Nacos](../infrastructure/nacos.md)).

## Spring Boot Starter

If you're on Spring Boot, prefer `agentscope-spring-boot-starter-a2a-server` — it auto-configures the server and controller. See [Quickstart](../../docs/quickstart.md).