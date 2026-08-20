# Chat Completions Web — 实践指南

> 本文档是实践指南，不属于某个独立 NuGet 包的官方功能。`AgentScope` 本身不提供 `ChatCompletionsWeb` 扩展包；以下思路基于 `AgentScope.Core` 的 `IOpenAIModel` 和 ASP.NET Core 基础设施。

## 目标

将 AgentScope Agent 包装为 OpenAI Chat Completions 兼容 HTTP 接口，让 OpenAI SDK、LangChain、ChatBox 等客户端可以直接调用。

## 思路

AgentScope 的 `OpenAIModel` 本身就是 OpenAI Chat Completions API 的客户端。若要让 AgentScope Agent **对外暴露**与 OpenAI 兼容的端点，只需在 ASP.NET Core Controller 中手动转换请求并流式输出 SSE。

## 示例：ASp.NET Core 控制器

```csharp
using System.Text.Json;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

[ApiController]
public class ChatController : ControllerBase
{
    private readonly IAgent _agent;

    public ChatController(IAgent agent)
    {
        _agent = agent;
    }

    [HttpPost("/v1/chat/completions")]
    public async Task ChatCompletions([FromBody] JsonElement body)
    {
        Response.ContentType = "text/event-stream";

        // 提取用户消息
        var messages = body.GetProperty("messages");
        var last = messages.EnumerateArray().Last();
        var text = last.GetProperty("content").GetString() ?? "";

        var msg = Msg.Builder().Role("user").TextContent(text).Build();

        // 调用 Agent
        var result = await _agent.CallAsync(new[] { msg });

        // 输出 OpenAI 格式
        var response = new
        {
            id = $"chatcmpl-{Guid.NewGuid():N}",
            @object = "chat.completion.chunk",
            choices = new[]
            {
                new
                {
                    delta = new { content = result.GetTextContent() },
                    index = 0,
                    finish_reason = "stop"
                }
            }
        };

        await Response.WriteAsync($"data: {JsonSerializer.Serialize(response)}\n\n");
        await Response.WriteAsync("data: [DONE]\n\n");
    }
}
```

## 流式输出

如需完整的 SSE 流式输出（逐 token 推送），可以遍历 Agent 的流事件：

```csharp
await foreach (var evt in _agent.StreamEventsAsync(new[] { msg }))
{
    // 将 evt 转换为 OpenAI delta chunk
    // 参考 AG-UI 的事件映射逻辑
}
```

## 模型路由

OpenAI 客户端通常携带 `model` 字段，可在 Controller 层做路由：

```csharp
var modelName = body.GetProperty("model").GetString();
var targetAgent = modelName switch
{
    "gpt-4o" => myAgent,
    "translator" => translatorAgent,
    _ => defaultAgent
};
```

## 对比 AG-UI

| 特性 | AG-UI | Chat Completions Web |
| --- | --- | --- |
| 面向 | 前端 UI 可视化 | 标准 LLM 接入 |
| 事件粒度 | 细粒度（推理、工具调用、状态） | 仅文本/token |
| 协议 | AG-UI Protocol | OpenAI Chat Completions |
| 实现位置 | `AgentScope.Core.AgUI` | 实践指南，手工实现 |
