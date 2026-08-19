# Higress AI 网关

`AgentScope.Extensions.Higress` 把 [Higress](https://higress.io/) 上以 MCP（Model Context Protocol）方式发布的工具引入 AgentScope。Higress 在网关层做了工具搜索、鉴权、限流、可观测；Agent 这边只负责调用对应工具。

## 何时使用

- 已经在用 Higress 作为 AI 网关，希望把网关上的工具直接喂给 Agent。
- 想把工具治理（路由、鉴权、配额）和 Agent 业务逻辑解耦。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Higress" Version="$(AgentScopeVersion)" />
```

## 快速上手

```csharp
using AgentScope.Extensions.Higress;

// 1) 通过 Higress 网关发布的 MCP endpoint 创建客户端
HigressMcpClientWrapper client = HigressMcpClientBuilder
    .Create("higress")
    .StreamableHttpEndpoint("http://gateway/mcp-servers/union-tools-search")
    .Build();

// 2) 注册到 HigressToolkit（Toolkit 子类，额外缓存了 Higress 客户端引用）
HigressToolkit toolkit = new();
toolkit.RegisterMcpClient(client).Wait();

// 3) 给 Agent 使用
ReActAgent agent = ReActAgent.Builder()
    .Name("Assistant")
    .Model(model)
    .Toolkit(toolkit)
    .Build();
```

## 选择性启用工具

`HigressToolkit` 沿用了 `Toolkit` 的 fluent registration API，可以按 group / 名称白名单细粒度控制：

```csharp
toolkit.Registration()
    .McpClient(client)
    .EnableTools(new List<string> { "search-doc", "fetch-url" })
    .Group("knowledge")
    .Apply();
```

## 拿到底层 MCP 客户端

如果你需要直接调用 Higress 提供的扩展 API（比如工具搜索 `HigressToolSearchResult`）：

```csharp
HigressMcpClientWrapper higressClient = toolkit.GetHigressMcpClient();
```

> Higress 上的工具治理（鉴权、限流、路由、可观测）由网关侧完成，无需在 Agent 这边重复实现。
