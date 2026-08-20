# Chat Completions Web — Practice Guide

> This document is a practice guide, not a feature of a standalone NuGet package. AgentScope does not ship a `ChatCompletionsWeb` extension package; the approach below is based on `AgentScope.Core`'s infrastructure and ASP.NET Core.

## Goal

Expose an AgentScope Agent behind an OpenAI Chat Completions–compatible HTTP API so that OpenAI SDKs, LangChain, ChatBox, etc. can connect as if talking to OpenAI.

## Approach

AgentScope's `OpenAIModel` consumes the OpenAI Chat Completions API as a client. To **expose** an AgentScope Agent as an OpenAI-compatible endpoint, manually translate requests and stream responses in an ASP.NET Core controller.

## Example: ASP.NET Core Controller

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

        var messages = body.GetProperty("messages");
        var last = messages.EnumerateArray().Last();
        var text = last.GetProperty("content").GetString() ?? "";

        var msg = Msg.Builder().Role("user").TextContent(text).Build();
        var result = await _agent.CallAsync(new[] { msg });

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

## Streaming

For token-by-token SSE streaming, iterate over the Agent's stream events:

```csharp
await foreach (var evt in _agent.StreamEventsAsync(new[] { msg }))
{
    // Convert evt to an OpenAI delta chunk
}
```

## Model Routing

OpenAI clients send a `model` field — route at the controller layer:

```csharp
var modelName = body.GetProperty("model").GetString();
var targetAgent = modelName switch
{
    "gpt-4o" => myAgent,
    "translator" => translatorAgent,
    _ => defaultAgent
};
```

## Comparison with AG-UI

| Aspect | AG-UI | Chat Completions Web |
| --- | --- | --- |
| Audience | Front-end UI visualization | Standard LLM integration |
| Event granularity | Fine (reasoning, tool calls, state) | Text/tokens only |
| Protocol | AG-UI Protocol | OpenAI Chat Completions |
| Implementation | `AgentScope.Core.AgUI` | Practice guide, manual |
