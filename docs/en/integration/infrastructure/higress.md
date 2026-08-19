# Higress AI Gateway

`AgentScope.Extensions.Higress` brings tools published as MCP (Model Context Protocol) on [Higress](https://higress.io/) into AgentScope. Higress handles tool search, auth, rate-limiting, and observability at the gateway layer; the Agent only invokes the resulting tools.

## When to use

- You already run Higress as an AI gateway and want to feed its tools to an Agent.
- You want tool governance (routing, auth, quotas) decoupled from Agent business logic.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Higress" Version="$(AgentScopeVersion)" />
```

## Quickstart

```csharp
using AgentScope.Extensions.Higress;

// 1) Create a client against an MCP endpoint published by Higress
HigressMcpClientWrapper client = HigressMcpClientBuilder
    .Create("higress")
    .StreamableHttpEndpoint("http://gateway/mcp-servers/union-tools-search")
    .Build();

// 2) Register with HigressToolkit (a Toolkit subclass that caches the Higress client)
HigressToolkit toolkit = new();
toolkit.RegisterMcpClient(client).Wait();

// 3) Use it from an Agent
ReActAgent agent = ReActAgent.Builder()
    .Name("Assistant")
    .Model(model)
    .Toolkit(toolkit)
    .Build();
```

## Selectively enable tools

`HigressToolkit` reuses the standard `Toolkit` fluent registration API for finer-grained control by group / allowlist:

```csharp
toolkit.Registration()
    .McpClient(client)
    .EnableTools(new List<string> { "search-doc", "fetch-url" })
    .Group("knowledge")
    .Apply();
```

## Access the underlying MCP client

If you need to call Higress-specific extensions (e.g. tool search via `HigressToolSearchResult`):

```csharp
HigressMcpClientWrapper higressClient = toolkit.GetHigressMcpClient();
```

> Tool governance (auth, rate-limiting, routing, observability) lives on the gateway, so you don't reimplement it on the Agent side.
