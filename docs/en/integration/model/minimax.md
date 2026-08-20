# MiniMax Model

MiniMax uses an OpenAI-compatible endpoint accessed through `OpenAIModel`. There is no dedicated model class.

## Constructor Example

```csharp
new OpenAIModel("MiniMax-M3", apiKey, "https://api.minimax.chat/v1")
```

## Minimal Example

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;

string key = System.Environment.GetEnvironmentVariable("MINIMAX_API_KEY");
var model = new OpenAIModel("MiniMax-M3", key,
    "https://api.minimax.chat/v1");

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
    .Model(new OpenAIModel("MiniMax-M3", key,
        "https://api.minimax.chat/v1"))
    .Build();
```

## Streaming

`OpenAIModel` implements `IStreamingChatModel`, and the MiniMax endpoint supports it:

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## Differences from Dedicated Models

- AgentScope has no `MiniMaxModel` class; all functionality comes from `OpenAIModel`.
- Suggested environment variable: `MINIMAX_API_KEY`.
- MiniMax-M3 supports adaptive thinking, configurable via `GenerateOptions` in the `defaultOptions` parameter.
- Custom formatters can be passed via `OpenAIModel`'s `formatter` parameter.

See [Model](../../docs/building-blocks/model.md) for the full interface reference.
