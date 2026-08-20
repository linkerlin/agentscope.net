# Higress AI 网关

`AgentScope.Extensions.Higress` 将 [Higress](https://higress.io/) 上以 MCP（Model Context Protocol）发布的远程工具引入 AgentScope。Higress 网关侧负责工具搜索、鉴权、限流和可观测；Agent 侧仅负责调用。

## 何时使用

- 已使用 Higress 作为 AI 网关，希望将网关上的工具直接注入 Agent。
- 希望将工具治理（路由、鉴权、配额）与 Agent 业务逻辑解耦。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Higress" Version="2.0.1" />
```

## 快速上手

```csharp
using AgentScope.Extensions.Higress;

// 创建 Higress MCP 客户端
var client = new HigressMcpClient(httpClient, "http://gateway/mcp-servers/union-tools-search");

// 创建 Toolkit 并发现远程工具
var toolkit = new HigressToolkit(client);
var tools = await toolkit.DiscoverAsync();

// 包装为本地 ITool，注入 Agent
var agent = new ReActAgent("Assistant", model, toolkit.AsTools().ToList());
```

## 核心 API

### HigressMcpClient

| 构造方法 | 说明 |
| --- | --- |
| `HigressMcpClient(HttpClient http, string baseUrl)` | 通过 `HttpClient` 连接 Higress MCP 端点 |

| 方法 | 说明 |
| --- | --- |
| `ListToolsAsync(CancellationToken ct)` | 列出网关注册的所有工具名称 |
| `CallToolAsync(string toolName, JsonElement args, CancellationToken ct)` | 远程调用指定工具 |

### HigressToolkit

| 构造方法 | 说明 |
| --- | --- |
| `HigressToolkit(HigressMcpClient client)` | 传入已初始化的 MCP 客户端 |

| 方法 | 说明 |
| --- | --- |
| `DiscoverAsync(CancellationToken ct)` | 发现并缓存网关上的工具列表，返回 `IReadOnlyList<HigressToolSearchResult>` |
| `Search(string keyword)` | 按关键词搜索已发现的工具 |
| `AsTools()` | 将已发现的工具包装为 `IEnumerable<ITool>`，供 Agent 使用 |

## 选择性启用工具

通过 `DiscoverAsync` 发现全部工具后，用 `Search` 筛选并手工构造 `ITool` 子集即可精确控制暴露给 Agent 的工具范围。

> 工具治理（鉴权、限流、路由、可观测）全部在 Higress 网关侧完成，Agent 侧无需重复实现。
