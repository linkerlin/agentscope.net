# AgentScope .NET Get Started

## 1. 模型配置

```csharp
using AgentScope.Core.Model.OpenAI;

IModel model = new OpenAIModel(
    "模型名称",
    "API Key 或 none（私有化部署无需 Key）",
    "API Base URL（私有化端点地址）");
```

**支持的模型提供方：**

| 提供方 | 模型类 | 说明 |
|--------|--------|------|
| OpenAI 兼容 | `OpenAIModel` | 适用官方 API 及 vllm/Ollama 等兼容端点 |
| DashScope | `DashScopeModel` | 阿里云通义千问 |
| Anthropic | `AnthropicModel` | Claude 系列 |
| Gemini | `GeminiModel` | Google Gemini |
| Mock | `MockModel` | 无真实 Key 时本地测试 |

---

## 2. 构建 HarnessAgent

```csharp
using AgentScope.Harness;
using AgentScope.Harness.Middleware;

HarnessAgent agent = new HarnessAgentBuilder()
    .WithName("agent-name")
    .WithSystemPrompt("你是一个帮助用户做笔记的助手。")
    .WithModel(model)
    .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
    .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))
    .Build();
```

**关键配置项：**

| 方法 | 说明 |
|------|------|
| `WithName(name)` | Agent 名称 |
| `WithSystemPrompt(prompt)` | 系统提示词 |
| `WithModel(model)` | 模型实例 |
| `WithWorkspaceRoot(path)` | 工作区根路径（持久化对话状态） |
| `WithMiddleware(mw)` | 添加中间件（如 CompactionMiddleware 控制上下文窗口） |

---

## 3. 运行时上下文

```csharp
RuntimeContext ctx = RuntimeContext.Empty
    .WithUserId("alice")
    .WithSessionId("demo-session");
```

**同一 (userId, sessionId) 跨调用恢复对话状态。**

---

## 4. 非流式调用：CallAsync

`CallAsync` 一次性返回最终回复消息 `Msg`：

```csharp
// 构造用户消息
Msg userMsg = Msg.Builder()
    .Role("user")
    .TextContent("我叫张三，今天准备一个关于 ReAct 的技术分享。")
    .Build();

// 调用 Agent（非流式）
Msg reply = await agent.CallAsync(userMsg, ctx);
Console.WriteLine($"Assistant: {reply.GetTextContent()}");
```

**适用场景：** 只需要最终结果，不关心中间过程。

---

## 5. 流式调用：StreamEventsAsync

`StreamEventsAsync` 逐事件输出，实时展示推理/行动/摘要过程：

```csharp
await foreach (var ev in agent.StreamEventsAsync(userMsg, ctx))
{
    // ev.Type  — 事件类型
    // ev.Message?.GetTextContent() — 事件携带的文本内容
    // ev.IsLast — 是否为最后一个事件

    switch (ev.Type)
    {
        case EventType.ReasoningChunk:
            // 模型推理过程的增量文本
            Console.Write(ev.Message?.GetTextContent());
            break;

        case EventType.ReasoningStart:
            Console.WriteLine("\n[推理开始]");
            break;

        case EventType.ReasoningFinish:
            Console.WriteLine("\n[推理结束]");
            break;

        case EventType.SummaryChunk:
            // 最终摘要的增量文本
            Console.Write(ev.Message?.GetTextContent());
            break;

        case EventType.ActingChunk:
            // 工具执行结果文本
            Console.WriteLine($"[工具结果] {ev.Message?.GetTextContent()}");
            break;

        case EventType.Error:
            Console.WriteLine($"[错误] {ev.Message?.GetTextContent()}");
            break;
    }
}
```

### 事件类型完整列表

| 事件类型 | 含义 | 携带文本 |
|---------|------|---------|
| `ReasoningStart` | 推理开始 | ❌ |
| `ReasoningChunk` | 推理增量文本 | ✅ |
| `ReasoningFinish` | 推理结束 | ❌ |
| `ActingStart` | 行动开始 | ❌ |
| `ActingChunk` | 工具执行结果 | ✅ |
| `ActingFinish` | 行动结束 | ❌ |
| `SummaryStart` | 摘要开始 | ❌ |
| `SummaryChunk` | 摘要增量文本 | ✅ |
| `SummaryFinish` | 摘要结束 | ❌ |
| `Error` | 错误事件 | ✅ |

---

## 6. 带记忆的多轮对话

同一 `(userId, sessionId)` 下的多次 `CallAsync` / `StreamEventsAsync` 会自动恢复之前对话状态：

```csharp
RuntimeContext ctx = RuntimeContext.Empty
    .WithUserId("alice")
    .WithSessionId("demo-session");

// 第一轮
Msg first = Msg.Builder().Role("user").TextContent("我叫张三，今天准备 ReAct 技术分享。").Build();
var reply1 = await agent.CallAsync(first, ctx);
Console.WriteLine($"第一轮: {reply1.GetTextContent()}");

// 第二轮 —— 同 sessionId，自动记住我的名字和任务
Msg second = Msg.Builder().Role("user").TextContent("我叫什么？我今天要干什么？").Build();
var reply2 = await agent.CallAsync(second, ctx);
Console.WriteLine($"第二轮: {reply2.GetTextContent()}");
// 输出：您叫张三，今天要准备 ReAct 技术分享。
```

---

## 7. 完整示例

参见 `examples/AgentScope.Lab/GetStarted.cs` 与 `examples/AgentScope.Lab/Program.cs`。
