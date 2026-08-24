# Higress AI Gateway

`AgentScope.Extensions.Higress` brings tools published as MCP (Model Context Protocol) on [Higress](https://higress.io/) into AgentScope. The gateway handles tool search, auth, rate-limiting, and observability; the Agent only invokes the resulting tools.

## When to use

- You already run Higress as an AI gateway and want to feed its tools to an Agent.
- You want tool governance (routing, auth, quotas) decoupled from Agent business logic.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Higress" Version="2.0.1" />
```

## Quickstart

```csharp
using AgentScope.Extensions.Higress;

// Create a Higress MCP client
var client = new HigressMcpClient(httpClient, "http://gateway/mcp-servers/union-tools-search");

// Create a toolkit and discover remote tools
var toolkit = new HigressToolkit(client);
var tools = await toolkit.DiscoverAsync();

// Wrap as local ITool instances and inject into an Agent
var agent = new ReActAgent("Assistant", model, toolkit.AsTools().ToList());
```

## Core API

### HigressMcpClient

| Constructor | Description |
| --- | --- |
| `HigressMcpClient(HttpClient http, string baseUrl)` | Connect to a Higress MCP endpoint via HttpClient |

| Method | Description |
| --- | --- |
| `ListToolsAsync(CancellationToken ct)` | List all tool names registered on the gateway |
| `CallToolAsync(string toolName, JsonElement args, CancellationToken ct)` | Invoke a remote tool |

### HigressToolkit

| Constructor | Description |
| --- | --- |
| `HigressToolkit(HigressMcpClient client)` | Pass an initialized MCP client |

| Method | Description |
| --- | --- |
| `DiscoverAsync(CancellationToken ct)` | Discover and cache tools from the gateway; returns `IReadOnlyList<HigressToolSearchResult>` |
| `Search(string keyword)` | Search cached tools by keyword |
| `AsTools()` | Wrap discovered tools as `IEnumerable<ITool>` for Agent use |

> Tool governance (auth, rate-limiting, routing, observability) lives on the gateway — no need to reimplement on the Agent side.
