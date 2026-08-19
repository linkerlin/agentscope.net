# Chat Completions Web

`AgentScope.Extensions.ChatCompletionsWeb` 把 AgentScope Agent 包装成 [OpenAI Chat Completions](https://platform.openai.com/docs/api-reference/chat) 兼容接口，让 OpenAI SDK、LangChain、LlamaIndex、ChatBox 等客户端"以为自己在调 OpenAI"。

## 何时使用

- 想把 Agent 变成"标准 LLM"暴露给已有客户端，无需改对端代码。
- 希望保留流式输出、工具调用过程，符合 OpenAI 的 SSE 协议格式。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.ChatCompletionsWeb" Version="$(AgentScopeVersion)" />
```

注意：本扩展**只**提供框架无关的核心适配器，真正的 HTTP/SSE 路由请自行写 Controller。

## 核心适配器

```csharp
using AgentScope.Core.ChatCompletions.Streaming;
using AgentScope.Core.ChatCompletions.Model;

ChatCompletionsStreamingAdapter adapter = new();

// 把 OpenAI 风格 Request 转成 Agent 调用 + 反向把事件流转回 OpenAI chunks
IAsyncEnumerable<ChatCompletionsChunk> stream = adapter.Stream(agent, request);
```

适配器把 AgentScope 的 `Event` 流（含 `REASONING`、`TOOL_RESULT` 等）转成 OpenAI 兼容的 `ChatCompletionsChunk`，包括：

- 文本增量 → `delta.Content`
- 工具调用 → `delta.ToolCalls[]`
- 流结束 → 带 `FinishReason` 的 chunk

## 在 ASP.NET Core 里暴露 SSE

```csharp
[ApiController]
public class ChatController : ControllerBase
{
    private readonly ChatCompletionsStreamingAdapter _adapter = new();
    private readonly Agent _agent;

    public ChatController(Agent agent)
    {
        _agent = agent;
    }

    [HttpPost("/v1/chat/completions")]
    public async Task Chat([FromBody] ChatCompletionsRequest req)
    {
        Response.ContentType = "text/event-stream";
        await foreach (var chunk in _adapter.Stream(_agent, req))
        {
            await Response.WriteAsync(ToSseLine(chunk));
            await Response.Body.FlushAsync();
        }
    }
}
```

## 模型对照表

OpenAI 客户端发起调用时通常会带 `model` 字段，可在控制器层做映射：

```csharp
string model = req.Model;   // 例如 "gpt-4o"，路由到不同 Agent
Agent target = agentRegistry.Lookup(model);
IAsyncEnumerable<ChatCompletionsChunk> stream = _adapter.Stream(target, req);
```

## 适合搭配

- **AG-UI**：偏 Web 前端可视化，关注事件粒度的 UI 渲染。
- **Chat Completions Web**：偏标准 LLM 接入，只关心 OpenAI 兼容。
