# DeepSeek Model

`AgentScope.Core.Model.DeepSeek.DeepSeekModel` connects to the DeepSeek API, extending `OpenAIModel`.

## Constructor

```csharp
DeepSeekModel(string modelName = "deepseek-chat", string? apiKey = null)
```

When `apiKey` is omitted, automatically reads `DEEPSEEK_API_KEY` from environment.

## Minimal Example

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.DeepSeek;

var model = new DeepSeekModel("deepseek-chat");

string response = await model.GenerateAsync(
    new ModelRequest
    {
        Messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("Hello").Build()
        }
    }).Result.Text;
```

## Agent Integration

```csharp
using AgentScope.Core.Model.DeepSeek;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new DeepSeekModel("deepseek-chat"))
    .Build();
```

## Streaming

`DeepSeekModel` extends `OpenAIModel` and implements `IStreamingChatModel`:

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## Notes

- `DeepSeekModel` inherits from `OpenAIModel`, which supports more constructor parameters (custom `baseUrl`, `client`, `formatter`, `defaultOptions`), but the `DeepSeekModel` constructor only exposes `modelName` and `apiKey`.
- For custom endpoints, use `new OpenAIModel("deepseek-chat", key, "https://api.deepseek.com")` directly.

See [Model](../../docs/building-blocks/model.md) for the full interface reference.
