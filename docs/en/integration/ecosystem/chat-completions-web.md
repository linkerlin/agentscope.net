# Chat Completions Web

`AgentScope.Extensions.ChatCompletionsWeb` exposes an AgentScope Agent behind an [OpenAI Chat Completions](https://platform.openai.com/docs/api-reference/chat)-compatible API, so OpenAI SDKs, LangChain, LlamaIndex, ChatBox, etc. can connect as if they were talking to OpenAI.

## When to use

- You want to expose your Agent as a "standard LLM" without modifying clients.
- You need streaming with tool-call events that match the OpenAI SSE format.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.ChatCompletionsWeb" Version="$(AgentScopeVersion)" />
```

Note: this module ships only the framework-agnostic core adapter — wire HTTP/SSE through your own controller.

## Core adapter

```csharp
using AgentScope.Core.ChatCompletions.Streaming;
using AgentScope.Core.ChatCompletions.Model;

ChatCompletionsStreamingAdapter adapter = new();

// Convert OpenAI-style request → Agent invocation, and Agent events → OpenAI chunks
IAsyncEnumerable<ChatCompletionsChunk> stream = adapter.Stream(agent, request);
```

The adapter converts AgentScope's `Event` stream (including `REASONING` and `TOOL_RESULT`) into OpenAI-compatible `ChatCompletionsChunk` objects:

- Text deltas → `delta.Content`
- Tool calls → `delta.ToolCalls[]`
- Stream end → a chunk with `FinishReason`

## Expose as SSE in ASP.NET Core

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

## Model routing

OpenAI clients send a `model` field; route at the controller layer:

```csharp
string model = req.Model;   // e.g. "gpt-4o"; route to different Agents
Agent target = agentRegistry.Lookup(model);
IAsyncEnumerable<ChatCompletionsChunk> stream = _adapter.Stream(target, req);
```

## Pairs well with

- **AG-UI** for fine-grained UI rendering with event semantics.
- **Chat Completions Web** for plain LLM-style integrations focused on OpenAI compatibility.
