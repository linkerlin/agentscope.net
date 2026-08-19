# A2A（Agent-to-Agent）

`agentscope-extensions-a2a` 实现了 [A2A 协议](https://a2aproject.github.io/A2A/)，包含两个子模块：

- `agentscope-extensions-a2a-client`：把一个远端 A2A Agent 包装成本地 `Agent`，可以直接 `agent.call(...)`。
- `agentscope-extensions-a2a-server`：把本地 `ReActAgent` 暴露成 A2A Server。

两个模块完全独立，可以单用其一。

## 客户端：调用远端 A2A Agent

### 添加依赖

```xml
<dependency>
    <groupId>io.agentscope</groupId>
    <artifactId>agentscope-extensions-a2a-client</artifactId>
    <version>${agentscope.version}</version>
</dependency>
```

### 直接传入 AgentCard

```csharp
using io.a2a.spec.AgentCard;
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

### 用 well-known 自动获取 AgentCard

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

`A2aAgent` 是 `AgentBase` 的子类，可以像普通 Agent 一样组合到 Pipeline、MsgHub、Subagent 中。

## 服务端：把 ReActAgent 暴露成 A2A Server

### 添加依赖

```xml
<dependency>
    <groupId>io.agentscope</groupId>
    <artifactId>agentscope-extensions-a2a-server</artifactId>
    <version>${agentscope.version}</version>
</dependency>
```

### 构建 server

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

// 在你的 Web 框架里把请求委托给 transportWrapper
TransportWrapper wrapper = server.GetTransportWrapper("JSONRPC");
// ... Spring/Quarkus controller 转发到 wrapper.Handle(...)

server.PostEndpointReady();   // Web 服务监听端口后再调用，触发注册等动作
```

`AgentScopeA2aServer` 本身不监听端口、不暴露 endpoint，只负责构建组件、组装请求处理链；具体监听由你的 Web 框架完成。这样可以无缝接入 Spring Boot、Quarkus、Vert.x 等。

### 可选组件

- `TaskStore` / `QueueManager`：任务和事件队列存储，默认是内存实现，生产可换成持久化版本。
- `PushNotificationConfigStore` / `PushNotificationSender`：推送通知。
- `AgentRegistry`：把 `AgentCard` 注册到外部注册中心（如 Nacos，见 [Nacos](../infrastructure/nacos.md)）。

## Spring Boot Starter

如果你使用 Spring Boot，建议直接引入 `agentscope-spring-boot-starter-a2a-server`，自动装配上述 server 和控制器，详见[快速开始](../../docs/quickstart.md)。