# OpenAI Model

`AgentScope.Core.Model.OpenAI.OpenAIModel` connects to the OpenAI Chat Completions API. OpenAI-compatible endpoints (e.g. DeepSeek, GLM, Kimi, MiniMax) also use this class.

## Constructor

```csharp
OpenAIModel(string modelName, string? apiKey = null, string? baseUrl = null,
    OpenAIClient? client = null, OpenAIChatFormatter? formatter = null,
    GenerateOptions? defaultOptions = null)
```

## Minimal Example

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;

string key = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var model = new OpenAIModel("gpt-4o", key);

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
using AgentScope.Core.Model.OpenAI;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new OpenAIModel("gpt-4o", key))
    .Build();
```

## Streaming

`OpenAIModel` implements `IStreamingChatModel`:

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## Notes

- When `apiKey` is omitted, reads from the `OPENAI_API_KEY` environment variable.
- When `baseUrl` is omitted, uses the default OpenAI endpoint.
- Custom `OpenAIClient`, `OpenAIChatFormatter`, or `GenerateOptions` can be passed via the constructor.

See [Model](../../docs/building-blocks/model.md) for the full interface reference.
