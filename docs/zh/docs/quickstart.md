---
title: "快速开始"
description: "快速上手 AgentScope .NET 2.0 —— 用 HarnessAgent 跑通第一个智能体"
---

## 安装

AgentScope .NET 基于 **.NET 10.0**（`net10.0`），推荐使用 dotnet CLI。

### NuGet 包

`AgentScope.Harness` 是推荐的入口包，它在内部引用核心包 `AgentScope.Core`，把工作区管理、消息总线、文件系统抽象、子 Agent、中间件管道等工程能力打包进一个 Builder：

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Harness" Version="2.0.1" />
</ItemGroup>
```

如果只需要裸的 `ReActAgent` / `EnhancedReActAgent` 框架 API（不需要工作区 / 中间件管道 / 子 Agent），引用 `AgentScope.Core` 即可：

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Core" Version="2.0.1" />
</ItemGroup>
```

:::{note}
与许多框架不同，**所有模型提供商（OpenAI / DashScope / Anthropic / Gemini / DeepSeek / Ollama / Mock）都内置在 `AgentScope.Core` 中**，不存在 `AgentScope.Extensions.Model.*` 这样的模型扩展包，接入模型不需要额外安装任何包。
:::

## 第一个智能体

下面的例子使用与 `examples/AgentScope.Lab` 完全相同的构建 API（`HarnessAgentBuilder` + `RuntimeContext`），跑通三件事：**构建 HarnessAgent**、**通过 RuntimeContext 标识会话**、**多轮对话**。

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Harness;
using AgentScope.Harness.Middleware;

class FirstAgent
{
    static async Task Main(string[] args)
    {
        // 模型：直接构造，ApiKey 通常来自环境变量
        IModel model = new DashScopeModel(
            "qwen-plus",
            Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"));

        HarnessAgent agent = new HarnessAgentBuilder()
            .WithName("note-taker")
            .WithSystemPrompt("你是一个帮助用户做笔记的助手。")
            .WithModel(model)
            .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
            .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))
            .Build();

        // 运行时上下文：record 类型，用 With* 方法派生新实例
        RuntimeContext ctx = RuntimeContext.Empty
            .WithUserId("alice")
            .WithSessionId("demo-session");

        Msg first = Msg.Builder()
            .Role("user")
            .TextContent("我叫天宇，今天准备一个关于 ReAct 的技术分享。")
            .Build();
        Msg reply1 = await agent.CallAsync(first, ctx);
        Console.WriteLine($"Assistant: {reply1.GetTextContent()}");

        Msg second = Msg.Builder()
            .Role("user")
            .TextContent("我叫什么？我今天要干什么？")
            .Build();
        Msg reply2 = await agent.CallAsync(second, ctx);
        Console.WriteLine($"Assistant: {reply2.GetTextContent()}");
    }
}
```

没有 API Key 时，用 `MockModel` 走通流程（回显输入，不发起网络请求）：

```csharp
IModel model = MockModel.Builder().ModelName("mock-model").Build();
```

### 关键点说明

| API | 说明 |
|-----|------|
| `new HarnessAgentBuilder()...Build()` | `HarnessAgent` 只能通过 `HarnessAgentBuilder` 创建（构造函数是 internal） |
| `.WithModel(IModel)` | 必填；接受任何 `IModel` 实现，**没有字符串模型 id 的重载** |
| `.WithWorkspaceRoot(path)` | 便捷重载，等价于 `WithWorkspace(new WorkspaceManager(root, sandboxed: true))`；设置后自动启用工作区上下文注入、`@path` 展开与记忆维护中间件 |
| `.WithMiddleware(IHarnessMiddleware)` | 向管道追加自定义中间件（可多次调用），另有一批内置中间件自动装配 |
| `RuntimeContext.Empty.WithUserId(...).WithSessionId(...)` | `RuntimeContext` 是不可变 record，没有 Builder 类 |
| `agent.CallAsync(Msg, RuntimeContext)` | 驱动一次推理-行动循环，返回最终 `Msg`；`reply.GetTextContent()` 取文本 |

Builder 的全部可用方法见 [Harness 架构](./harness/architecture.md)。

### 流式查看推理与工具调用

把 `CallAsync(...)` 换成 `StreamEventsAsync(...)` 能实时拿到推理片段、工具调用等中间事件，适合 Web / TUI 渲染。返回的是 `IAsyncEnumerable<Event>`，用 `await foreach` 消费：

```csharp
using AgentScope.Core.Events;

await foreach (Event evt in agent.StreamEventsAsync(
    Msg.Builder().Role("user").TextContent("帮我把今天的关键点列三条。").Build(), ctx))
{
    if (evt.Type == EventType.ReasoningChunk && evt.Message != null)
    {
        // 模型输出的流式文本片段
        Console.Write(evt.Message.GetTextContent());
    }
    else if (evt.Type == EventType.ToolCallStart)
    {
        Console.WriteLine("\n[tool] 模型请求调用工具");
    }
    else if (evt.IsLast)
    {
        Console.WriteLine("\n[done]");
    }
}
```

事件类型由 `AgentScope.Core.Events.EventType` 枚举定义：`ReasoningStart/Chunk/Finish`、`ToolCallStart/Chunk/Finish`、`ActingStart/Chunk/Finish`、`SummaryStart/Chunk/Finish`、`Error`。完整说明见[消息与事件](./building-blocks/message-and-event.md)。

### 多用户并发

`HarnessAgent` 实例本身可以复用。通过 `RuntimeContext` 传入不同的 `UserId` / `SessionId`，每次调用互不干扰：

```csharp
// 应用启动时创建一个 agent 实例（单例即可）
HarnessAgent agent = new HarnessAgentBuilder()
    .WithName("note-taker")
    .WithSystemPrompt("你是一个帮助用户做笔记的助手。")
    .WithModel(model)
    .Build();

// 在 HTTP handler 中——不同请求传入不同 RuntimeContext
await agent.CallAsync(userInput, RuntimeContext.Empty
    .WithUserId(userId)
    .WithSessionId(sessionId));
```

`HarnessAgent` 还提供一个纯文本的便捷重载：`agent.CallAsync("你好", ctx)` 内部自动包装成 `Msg`。

需要跨进程恢复会话时，用 `EnhancedReActAgent` 的 `SaveTo` / `LoadFrom` 配合 `SessionManager`，或为记忆配置 `StateBackedMemory` + `IAgentStateStore`，详见[上下文与 AgentState](./building-blocks/context.md)。

## 接下来

- [智能体（Agent）](./building-blocks/agent.md) —— `EnhancedReActAgent` 的完整 Builder、调用、流式、结构化输出
- [模型](./building-blocks/model.md) —— 各提供商模型类的构造签名与流式接口
- [工具](./building-blocks/tool.md) —— `[Tool]` 特性注册、`Toolkit`、MCP 客户端
- [Harness 架构](./harness/architecture.md) —— `HarnessAgentBuilder` 全量方法与中间件管道
- [工作区](./harness/workspace.md) —— `AGENTS.md` / `MEMORY.md` / `skills/` 的目录布局与加载机制
