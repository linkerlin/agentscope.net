---
title: "Tool"
description: "ITool / Toolkit / [Tool] attribute registration / MCP client"
---

## Overview

Tools (`AgentScope.Core.Tool`) are capabilities that an agent can invoke during the acting phase. Three ways to define a tool:

1. **`[Tool]` attribute method** (recommended) — framework auto-generates JSON Schema;
2. **Inherit `ToolBase`** — manually implement `GetSchema()` and `ExecuteAsync()`;
3. **Directly implement `ITool`**.

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    Dictionary<string, object> GetSchema();
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
}

// Optional interface: supports true cancellation of underlying work via CancellationToken
public interface ICancellableTool : ITool
{
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken);
}
```

`ToolResult` provides two factory methods: `ToolResult.Ok(object result)` / `ToolResult.Fail(string error)`.

## Method 1: [Tool] Attribute

```csharp
using AgentScope.Core.Tool;

public class WeatherService
{
    [Tool(Name = "get_weather", Description = "Get weather for a specified city")]
    public string GetWeather(
        [ToolParam(Name = "city", Description = "City name")] string city,
        [ToolParam(Description = "Number of days", Required = false)] int days = 3)
        => $"It will be sunny in {city} for the next {days} days.";
}
```

Attribute details:

- `[Tool]`: `Name` (default method name), `Description` (default auto-generated), `Strict`, `ReadOnly`, `ExternalTool` (all default false).
- `[ToolParam]`: `Name` (default parameter name), `Description`, `Required` (**default true**; optional parameters must explicitly set `Required = false`).

Registering with `Toolkit`:

```csharp
var toolkit = new Toolkit();
toolkit.RegisterTool(new WeatherService());   // Scans [Tool] methods on the instance
toolkit.RegisterTool<MathTools>();            // Scans static [Tool] methods on type T
```

> Note the method name is **`RegisterTool`** (singular); use **`AddTool`** to register an existing `ITool` instance.

## Method 2: Inherit ToolBase

```csharp
public class EchoTool : ToolBase
{
    public EchoTool() : base("echo", "Echoes the input content") { }

    public override Dictionary<string, object> GetSchema() => new()
    {
        ["name"] = Name,
        ["description"] = Description,
        ["parameters"] = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["message"] = new Dictionary<string, object> { ["type"] = "string" }
            },
            ["required"] = new List<string> { "message" }
        }
    };

    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        => Task.FromResult(parameters.TryGetValue("message", out var m)
            ? ToolResult.Ok(m?.ToString() ?? "")
            : ToolResult.Fail("Missing parameter message"));
}
```

## Toolkit

`Toolkit` is the tool registration center facade:

| Member | Signature | Description |
|--------|-----------|-------------|
| `AddTool` | `Toolkit AddTool(ITool tool, string? group = null)` | Registers a single tool, optionally with a group name |
| `AddGroup` | `Toolkit AddGroup(ToolGroup group)` | Registers a tool group |
| `AddSkillGroup` | `Toolkit AddSkillGroup(SkillToolGroup skillGroup)` | Registers a skill tool group |
| `ActivateGroup` / `DeactivateGroup` | `Toolkit ActivateGroup(string name)` | Activates / deactivates a group |
| `GetActiveTools` | `IReadOnlyList<ITool>` | **Returns all tools when no group is active**; returns only active group tools when groups are active |
| `GetActiveToolSchemas` | `List<Dictionary<string, object>>` | Schemas of currently active tools |
| `Resolve` | `ITool? Resolve(string name)` | Looks up by name |
| `RegisterTool` | `Toolkit RegisterTool(object toolObject)` / `static Toolkit RegisterTool<T>()` | Scans `[Tool]` attributes |
| `CallToolsAsync` | `Task<List<ToolResultBlock>> CallToolsAsync(List<ToolUseBlock>, ExecutionConfig?)` | Batch execution |
| `AllTools` / `Groups` | Properties | All tools / groups |

Tool group example:

```csharp
var toolkit = new Toolkit()
    .AddTool(new CalculatorTool(), group: "math")
    .AddGroup(new ToolGroup("search", "Web search", isActive: true));
```

`ToolGroupManager` (Builder's `ToolGroupManager(...)` / `AddToolGroup(...)`) maintains group activation states, which are persisted with `Session` (`ToolkitState`).

## Built-in Tools

| Tool | Namespace | Name | Description |
|------|-----------|------|-------------|
| `CalculatorTool` | `AgentScope.Core.Tool` | `calculator` | Sum of two numbers (example tool) |
| `GetTimeTool` | `AgentScope.Core.Tool` | `get_time` | Current time |
| `WebSearchTool` / `MockWebSearchTool` | `AgentScope.Core.Tool` | `web_search` | Web search (falls back to mock when no key is available) |
| `CodeExecutionTool` / `SafeCodeExecutionTool` | `AgentScope.Core.Tool` | `code_execution` | Code execution (with module allow/blocklist) |
| `ReadFileTool` / `WriteFileTool` | `AgentScope.Core.Tool.File` | `read_file` / `write_file` | File read/write constrained by `FileToolUtils` sandbox |
| `ShellCommandTool` | `AgentScope.Core.Tool.Coding` | `shell_command` | Shell command execution (Windows/Unix command validators) |

```csharp
// File tools only allow current directory + temp directory by default; can be adjusted globally:
FileToolUtils.AllowedRoots = new[] { @"D:\data" };
```

## ToolExecutor: Retry and Timeout

```csharp
var executor = new ToolExecutor(
    maxAttempts: 3,
    timeout: TimeSpan.FromSeconds(30),
    retryDelay: TimeSpan.FromSeconds(1),
    shouldRetry: (ex, attempt) => ex is HttpRequestException);

ToolResult result = await executor.ExecuteAsync(tool, parameters, ct);
```

If the tool implements `ICancellableTool`, timeout will truly cancel the underlying work; otherwise only stops waiting.

## MCP Client

`AgentScope.Core.MCP` has a built-in MCP (Model Context Protocol) client supporting three transports:

```csharp
using AgentScope.Core.MCP;

// Stdio (local process)
IMcpClient stdio = McpClientBuilder.Create()          // Note: static factory, constructor is private
    .Named("fs-server")
    .UseStdio("node", "mcp-server.js")
    .WithWorkingDirectory(@"D:\mcp")
    .Build();

// Streamable HTTP / SSE (remote service)
IMcpClient http = McpClientBuilder.Create()
    .Named("amap")
    .UseStreamableHttp("https://mcp.amap.com/mcp")
    .WithApiKey("YOUR_KEY")                           // Authorization: Bearer
    .WithRequestTimeout(TimeSpan.FromSeconds(60))
    .Build();

IMcpClient sse = McpClientBuilder.Create()
    .UseSse("https://example.com/mcp/sse")
    .Build();

// Register with McpManager and discover tools
var manager = new McpManager();
manager.RegisterClient(http);
IReadOnlyList<ITool> tools = await manager.CreateToolsAsync();   // Auto-initialize and discover

var toolkit = new Toolkit();
foreach (var tool in tools)
    toolkit.AddTool(tool);
```

`McpClientBuilder` chain methods: `Create()` → `Named(string)` → `UseStdio(command, args?)` / `UseStreamableHttp(url)` / `UseSse(url)` (choose one) → optional `WithApiKey` / `WithWorkingDirectory` (Stdio only) / `WithHttpClient` (HTTP/SSE only) / `WithRequestTimeout` → `Build()`.

`IMcpClient` interface: `InitializeAsync()`, `ListToolsAsync()`, `CallToolAsync(name, args)`.

## Integration with Agent

```csharp
EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(model)
    .AddTool(new EchoTool())
    .Build();

// HarnessAgent side: WithToolkit to bring in all tools at once
var toolkit = new Toolkit().AddTool(new EchoTool());
HarnessAgent harness = new HarnessAgentBuilder()
    .WithModel(model)
    .WithToolkit(toolkit)
    .Build();
```

## Related Documentation

- [Permission System](./permission-system.md) — Three-state decisions before tool calls
- [Harness Skill](../harness/skill.md) — Skill tool groups generated from Markdown skills
