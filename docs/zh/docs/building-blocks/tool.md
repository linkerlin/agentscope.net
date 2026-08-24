---
title: "工具"
description: "ITool / Toolkit / [Tool] 特性注册 / MCP 客户端"
---

## 概述

工具（`AgentScope.Core.Tool`）是智能体在行动阶段可调用的能力。三种定义方式：

1. **`[Tool]` 特性方法**（推荐）——框架自动生成 JSON Schema；
2. **继承 `ToolBase`**——手工实现 `GetSchema()` 与 `ExecuteAsync()`；
3. **直接实现 `ITool`**。

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    Dictionary<string, object> GetSchema();
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
}

// 可选接口：支持通过 CancellationToken 真正中止底层工作
public interface ICancellableTool : ITool
{
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken);
}
```

`ToolResult` 提供 `ToolResult.Ok(object result)` / `ToolResult.Fail(string error)` 两个工厂。

## 方式一：[Tool] 特性方法

```csharp
using AgentScope.Core.Tool;

public class WeatherService
{
    [Tool(Name = "get_weather", Description = "获取指定城市的天气")]
    public string GetWeather(
        [ToolParam(Name = "city", Description = "城市名")] string city,
        [ToolParam(Description = "天数", Required = false)] int days = 3)
        => $"{city} 未来 {days} 天晴。";
}
```

特性说明：

- `[Tool]`：`Name`（默认方法名）、`Description`（默认自动生成）、`Strict`、`ReadOnly`、`ExternalTool`（均默认 false）。
- `[ToolParam]`：`Name`（默认参数名）、`Description`、`Required`（**默认 true**，可选参数要显式写 `Required = false`）。

注册到 `Toolkit`：

```csharp
var toolkit = new Toolkit();
toolkit.RegisterTool(new WeatherService());   // 扫描实例上的 [Tool] 方法
toolkit.RegisterTool<MathTools>();            // 扫描类型 T 的静态 [Tool] 方法
```

> 注意方法名是 **`RegisterTool`**（单数）；注册现成的 `ITool` 实例用 **`AddTool`**。

## 方式二：继承 ToolBase

```csharp
public class EchoTool : ToolBase
{
    public EchoTool() : base("echo", "回显输入内容") { }

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
            : ToolResult.Fail("缺少参数 message"));
}
```

## Toolkit

`Toolkit` 是工具注册中心门面：

| 成员 | 签名 | 说明 |
|------|------|------|
| `AddTool` | `Toolkit AddTool(ITool tool, string? group = null)` | 注册单个工具，可指定组名 |
| `AddGroup` | `Toolkit AddGroup(ToolGroup group)` | 注册工具组 |
| `AddSkillGroup` | `Toolkit AddSkillGroup(SkillToolGroup skillGroup)` | 注册技能工具组 |
| `ActivateGroup` / `DeactivateGroup` | `Toolkit ActivateGroup(string name)` | 激活 / 停用组 |
| `GetActiveTools` | `IReadOnlyList<ITool>` | **无激活组时返回全部工具**；有激活组时只返回激活组内的 |
| `GetActiveToolSchemas` | `List<Dictionary<string, object>>` | 当前激活工具的 Schema |
| `Resolve` | `ITool? Resolve(string name)` | 按名称查找 |
| `RegisterTool` | `Toolkit RegisterTool(object toolObject)` / `static Toolkit RegisterTool<T>()` | 扫描 `[Tool]` 特性 |
| `CallToolsAsync` | `Task<List<ToolResultBlock>> CallToolsAsync(List<ToolUseBlock>, ExecutionConfig?)` | 批量执行 |
| `AllTools` / `Groups` | 属性 | 全量工具 / 组 |

工具分组示例：

```csharp
var toolkit = new Toolkit()
    .AddTool(new CalculatorTool(), group: "math")
    .AddGroup(new ToolGroup("search", "联网搜索", isActive: true));
```

`ToolGroupManager`（Builder 的 `ToolGroupManager(...)` / `AddToolGroup(...)`）维护分组激活状态，且会随 `Session` 持久化（`ToolkitState`）。

## 内置工具

| 工具 | 命名空间 | 名称 | 说明 |
|------|----------|------|------|
| `CalculatorTool` | `AgentScope.Core.Tool` | `calculator` | 两数求和（示例工具） |
| `GetTimeTool` | `AgentScope.Core.Tool` | `get_time` | 当前时间 |
| `WebSearchTool` / `MockWebSearchTool` | `AgentScope.Core.Tool` | `web_search` | 网页搜索（无 Key 时降级模拟） |
| `CodeExecutionTool` / `SafeCodeExecutionTool` | `AgentScope.Core.Tool` | `code_execution` | 代码执行（带模块黑白名单） |
| `ReadFileTool` / `WriteFileTool` | `AgentScope.Core.Tool.File` | `read_file` / `write_file` | 受 `FileToolUtils` 沙箱约束的文件读写 |
| `ShellCommandTool` | `AgentScope.Core.Tool.Coding` | `shell_command` | Shell 命令执行（Windows/Unix 命令验证器） |

```csharp
// 文件工具默认只允许 当前目录 + 临时目录，可全局调整：
FileToolUtils.AllowedRoots = new[] { @"D:\data" };
```

## ToolExecutor：重试与超时

```csharp
var executor = new ToolExecutor(
    maxAttempts: 3,
    timeout: TimeSpan.FromSeconds(30),
    retryDelay: TimeSpan.FromSeconds(1),
    shouldRetry: (ex, attempt) => ex is HttpRequestException);

ToolResult result = await executor.ExecuteAsync(tool, parameters, ct);
```

若工具实现 `ICancellableTool`，超时会真正取消底层工作；否则仅停止等待。

## MCP 客户端

`AgentScope.Core.MCP` 内置 MCP（Model Context Protocol）客户端，支持三种传输：

```csharp
using AgentScope.Core.MCP;

// Stdio（本地进程）
IMcpClient stdio = McpClientBuilder.Create()          // 注意：静态工厂，构造函数私有
    .Named("fs-server")
    .UseStdio("node", "mcp-server.js")
    .WithWorkingDirectory(@"D:\mcp")
    .Build();

// Streamable HTTP / SSE（远程服务）
IMcpClient http = McpClientBuilder.Create()
    .Named("amap")
    .UseStreamableHttp("https://mcp.amap.com/mcp")
    .WithApiKey("YOUR_KEY")                           // Authorization: Bearer
    .WithRequestTimeout(TimeSpan.FromSeconds(60))
    .Build();

IMcpClient sse = McpClientBuilder.Create()
    .UseSse("https://example.com/mcp/sse")
    .Build();

// 注册到 McpManager 并发现工具
var manager = new McpManager();
manager.RegisterClient(http);
IReadOnlyList<ITool> tools = await manager.CreateToolsAsync();   // 自动初始化并发现

var toolkit = new Toolkit();
foreach (var tool in tools)
    toolkit.AddTool(tool);
```

`McpClientBuilder` 链式方法：`Create()` → `Named(string)` → `UseStdio(command, args?)` / `UseStreamableHttp(url)` / `UseSse(url)`（三选一）→ 可选 `WithApiKey` / `WithWorkingDirectory`（仅 Stdio）/ `WithHttpClient`（仅 HTTP/SSE）/ `WithRequestTimeout` → `Build()`。

`IMcpClient` 接口：`InitializeAsync()`、`ListToolsAsync()`、`CallToolAsync(name, args)`。

## 与 Agent 集成

```csharp
EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(model)
    .AddTool(new EchoTool())
    .Build();

// HarnessAgent 侧：WithToolkit 一次性带入
var toolkit = new Toolkit().AddTool(new EchoTool());
HarnessAgent harness = new HarnessAgentBuilder()
    .WithModel(model)
    .WithToolkit(toolkit)
    .Build();
```

## 相关文档

- [权限系统](./permission-system.md) —— 工具调用前的三态决策
- [Harness 技能](../harness/skill.md) —— Markdown 技能生成的技能工具组
