# DashScope Model

`AgentScope.Core.Model.DashScope.DashScopeModel` connects to Alibaba Cloud DashScope Qwen models.

## Constructor

```csharp
DashScopeModel(string modelName, string? apiKey = null, string? baseUrl = null,
    DashScopeChatFormatter? formatter = null, GenerateOptions? defaultOptions = null)
```

Constants: `DefaultBaseUrl = "https://dashscope.aliyuncs.com"`, `ChatEndpoint = "/compatible-mode/v1/chat/completions"`.

## Minimal Example

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.DashScope;

string key = System.Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY");
var model = new DashScopeModel("qwen-plus", key);

string response = await model.GenerateAsync(
    new ModelRequest
    {
        Messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("Explain AgentScope").Build()
        }
    }).Result.Text;
```

## Agent Integration

```csharp
using AgentScope.Core.Model.DashScope;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new DashScopeModel("qwen-plus", key))
    .Build();
```

## Streaming

`DashScopeModel` implements `IStreamingChatModel`:

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## Notes

- When `apiKey` is omitted, read from the `DASHSCOPE_API_KEY` environment variable.
- The endpoint uses OpenAI-compatible format (`/compatible-mode/v1/chat/completions`).
- Custom `DashScopeChatFormatter` or `GenerateOptions` can be passed via the constructor.

See [Model](../../docs/building-blocks/model.md) for the full interface reference.
