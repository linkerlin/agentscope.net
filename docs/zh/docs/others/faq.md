---
title: "常见问题"
description: "FAQ：构建、模型、状态、流式事件"
---

## 构建与依赖

**Q: 需要安装模型扩展包吗？**

不需要。OpenAI / DashScope / Anthropic / Gemini / DeepSeek / Ollama / Mock 全部内置在 `AgentScope.Core`。

**Q: 项目目标框架？**

`net10.0`（源码 `TargetFramework` 为 net10.0，请使用 .NET 10 SDK 或更高版本构建）。

**Q: `AgentScope.Harness` 与 `AgentScope.Core` 如何选择？**

需要工作区 / 中间件管道 / 子 Agent / 转录等工程能力用 `AgentScope.Harness`；只需要裸推理循环用 `AgentScope.Core`。`AgentScope.Harness` 引用 `AgentScope.Core`。

## 模型

**Q: 为什么构造 `DashScopeModel` 报错？**

确认参数顺序：`DashScopeModel(string modelName, string? apiKey = null, ...)`。不要写 `new DashScopeModel(apiKey, "qwen-plus")`。

**Q: 如何在没有 API Key 时跑通流程？**

`MockModel.Builder().ModelName("mock-model").Build()`，回显最后一条消息，不发起网络请求。

**Q: Gemini 支持流式吗？**

`GeminiModel` 未实现 `IStreamingChatModel`，`EnhancedReActAgent` 会整段返回文本；如需流式请使用 OpenAI / DashScope / Anthropic / DeepSeek / Ollama。

## 消息与事件

**Q: `new UserMessage("文本")` 编译失败？**

`UserMessage` 没有单参文本构造。使用 `Msg.Builder().Role("user").TextContent("文本").Build()`，或 `new UserMessage(null, "文本")`。

**Q: `StreamEventsAsync` 返回什么？如何遍历？**

`IAsyncEnumerable<Event>`，用 `await foreach`：

```csharp
await foreach (Event evt in agent.StreamEventsAsync(msg))
{
    if (evt.Type == EventType.ReasoningChunk) Console.Write(evt.Message?.GetTextContent());
    if (evt.IsLast) break;
}
```

不是 `IObservable`，不能用 `Subscribe`。

**Q: `AgentEvent` 与 `Event` 什么关系？**

`Event`（`EventType` 枚举）是 ReAct 循环实际产出的流式事件；`AgentEvent` 是细粒度 record 层次（`TextBlockDeltaEvent` 等），由协议适配层（A2A / AgUI）使用。

## 智能体

**Q: `ReActAgent` 还能用吗？**

可以用但已标记 `[Obsolete]`，请迁移到 `EnhancedReActAgent`。

**Q: 如何让 agent 记得上一轮？**

`EnhancedReActAgent` 内部用 `Memory` 维护上下文：默认 `MemoryBase` 实例内保留；跨重启用 `SqliteMemory(path)` 或 `StateBackedMemory(store, initial, key)`；会话级保存恢复用 `agent.SaveTo(session, key)` / `agent.LoadIfExists(session, key)`。

**Q: 多个用户共用一个 agent 实例安全吗？**

安全。agent 是无状态引擎，每次调用通过 `RuntimeContext` 携带 `(UserId, SessionId)`；不同会话的调用互不干扰（记忆实现需自行保证隔离，如按会话选择 store）。

**Q: 如何中断正在执行的调用？**

`agent.Interrupt()`（或 `Interrupt(Msg)`），`EnhancedReActAgent` 在迭代检查点响应。

**Q: HITL 确认如何配置？**

Builder：`PermissionEngine(...)` + `ConfirmCallback(...)`（或 `AutoApproveOnAsk(true)`）。权限判定为 `Ask` 的工具调用会触发回调。

## Harness

**Q: `HarnessAgentBuilder` 为什么没有 `WithModel("dashscope:qwen-plus")`？**

2.0 移除字符串模型 id，模型必须传 `IModel` 实例：`WithModel(new DashScopeModel("qwen-plus", key))`。

**Q: 工作区是必须的吗？**

不是。`WithWorkspaceRoot(...)` 只是启用工作区上下文注入、`@path` 展开和记忆维护；不配置也能跑。

**Q: 哪些中间件会自动装配？**

SandboxLifecycle(50) → Subagents(300) → Teams(500) → Inbox(200) → PlanMode(400) → Compaction(700) → MemoryFlush(800) → AgentTrace(100) → Transcript(900)，工作区配置后追加 WorkspaceContext(25) / AtPathExpansion(20) / MemoryMaintenance(900)。完整 Order 表见 [Middleware](../building-blocks/middleware.md)。

**Q: 如何把 MCP 工具加入 agent？**

```csharp
var mcp = McpClientBuilder.Create().UseStdio("node", "mcp.js").Build();
var tools = await new McpManager() { /* RegisterClient */ }.CreateToolsAsync();
```

详见 [工具](../building-blocks/tool.md#mcp-客户端)。

## 存储与扩展

**Q: `AgentScope.Extensions.Redis` 包不存在？**

2.0 拆包：Redis 状态存储在 `AgentScope.Extensions.Store.Redis`（`RedisAgentStateStore`）。

**Q: 渠道扩展的 `IChannel` 与 Harness 的 `IChannel` 一样吗？**

不一样。`AgentScope.Extensions.Channel.*` 实现的是伞工程 `AgentScope.Extensions.Channel.IChannel`（webhook 客户端风格，含 `OnMessageReceived` 事件）；Harness 内部是 `AgentScope.Harness.Gateway.Channel.IChannel`（网关路由风格）。接入时需要适配。

**Q: Mem0 / Dify 等扩展是什么形态？**

`Mem.*` / `Rag.*` 是独立 HTTP 客户端类（如 `Mem0LongTermMemory(http, apiKey, baseUrl?)`），不实现 Core 接口，需要自行适配到 `ILongTermMemory` / RAG 层。
