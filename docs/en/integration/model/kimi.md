# Kimi Model

Kimi (Moonshot AI) uses an OpenAI-compatible endpoint accessed through `OpenAIModel`. There is no dedicated model class.

## Constructor Example

```csharp
new OpenAIModel("moonshot-v1-8k", apiKey, "https://api.moonshot.cn/v1")
```

## Minimal Example

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;

string key = System.Environment.GetEnvironmentVariable("MOONSHOT_API_KEY");
var model = new OpenAIModel("moonshot-v1-8k", key,
    "https://api.moonshot.cn/v1");

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
    .Model(new OpenAIModel("moonshot-v1-8k", key,
        "https://api.moonshot.cn/v1"))
    .Build();
```

## Streaming

`OpenAIModel` implements `IStreamingChatModel`, and the Kimi endpoint supports it:

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## Differences from Dedicated Models

- AgentScope has no `KimiModel` class; all functionality comes from `OpenAIModel`.
- Suggested environment variable: `MOONSHOT_API_KEY` or `KIMI_API_KEY`.
- Some Kimi models (e.g. kimi-k3) support thinking mode via `GenerateOptions` (e.g. `reasoning_effort`).
- Custom formatters can be passed via `OpenAIModel`'s `formatter` parameter.

See [Model](../../docs/building-blocks/model.md) for the full interface reference.
