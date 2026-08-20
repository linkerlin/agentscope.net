# A2A（Agent-to-Agent）

`AgentScope.Core` 在 `AgentScope.Core.A2A` 命名空间下实现了 [A2A 协议](https://a2aproject.github.io/A2A/)，包含客户端和服务端两部分。

## 客户端：调用远端 A2A Agent

### A2aAgent

`A2aAgent`（`AgentScope.Core.A2A.Client`）继承自 `AgentBase`，将远程 A2A Agent 包装成本地 Agent 实例。

```csharp
using AgentScope.Core.A2A.Client;
using AgentScope.Core.A2A.Client.Card;
using AgentScope.Core.Service.Discovery;

// 通过 IAgentCardResolver 解析远端 AgentCard
var card = new AgentCard("remote-1", "translator", "翻译服务", "http://other-service:8080");
var resolver = new FixedAgentCardResolver(card);
var agent = new A2aAgent("translator", resolver);

// 像普通 Agent 一样调用
var result = await agent.CallAsync(new Msg[] { Msg.Builder().Role("user").TextContent("你好").Build() });
```

### IAgentCardResolver

| 实现 | 说明 |
| --- | --- |
| `FixedAgentCardResolver(AgentCard card)` | 固定返回同一张 AgentCard |
| `WellKnownAgentCardResolver(HttpClient http)` | 从 `https://{agentName}/.well-known/agent-card.json` 获取 AgentCard |

### 接口

```csharp
public interface IAgentCardResolver
{
    Task<AgentCard?> ResolveAsync(string agentName, CancellationToken ct = default);
}
```

## 服务端：暴露本地 Agent 为 A2A Server

### AgentScopeA2aServer

```csharp
using AgentScope.Core.A2A.Server;
using AgentScope.Core.A2A.Server.Card;
using AgentScope.Core.A2A.Server.Executor.Runner;

// 创建 Runner（每次调用使用工厂创建新 Agent 实例）
var runner = new ReActAgentWithBuilderRunner(
    agentFactory: () => new ReActAgent("backend-agent", model),
    name: "backend-agent",
    description: "后端服务 Agent");

// 构建 A2A Server
var server = new AgentScopeA2aServer(runner, new ConfigurableAgentCard
{
    Name = "backend-agent",
    Description = "后端服务 Agent",
    Url = "http://localhost:5000"
});

// 注入外部注册中心（如 Nacos）
server.AddRegistry(nacosRegistry);

// 在 Web 框架中处理请求
// 将收到的 JSON-RPC 请求体传给 HandleRequestAsync
var response = await server.HandleRequestAsync(requestBody, headers);

// 服务启动后触发注册
await server.PostEndpointReadyAsync();
```

### 关键组件

| 类 | 命名空间 | 说明 |
| --- | --- | --- |
| `AgentScopeA2aServer` | `AgentScope.Core.A2A.Server` | 服务端入口，组装组件、处理请求 |
| `ConfigurableAgentCard` | `AgentScope.Core.A2A.Server.Card` | 可配置的 AgentCard Builder，调用 `Build()` 生成 `AgentCard` |
| `AgentScopeAgentCardConverter` | `AgentScope.Core.A2A.Server.Card` | 将 `ConfigurableAgentCard` 转换为 `AgentCard` |
| `AgentScopeAgentExecutor` | `AgentScope.Core.A2A.Server.Executor` | 执行器，支持阻塞 `ExecuteAsync` 和流式 `StreamAsync` |
| `ReActAgentWithBuilderRunner` | `AgentScope.Core.A2A.Server.Executor.Runner` | 默认 Runner，为每次请求创建新 Agent 实例 |
| `IAgentRunner` | `AgentScope.Core.A2A.Server.Executor.Runner` | Runner 接口，含 `StreamAsync` 和 `StopAsync` |

> `AgentScopeA2aServer` 本身不监听端口。它只负责组件组装和请求处理链，具体 HTTP 监听由你的 Web 框架完成。
