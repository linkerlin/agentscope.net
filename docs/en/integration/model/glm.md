# GLM Model

GLM (Zhipu AI / Z.AI) uses an OpenAI-compatible endpoint accessed through `OpenAIModel`. There is no dedicated model class.

## Constructor Example

```csharp
new OpenAIModel("glm-4-plus", apiKey, "https://open.bigmodel.cn/api/paas/v4")
```

## Minimal Example

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;

string key = System.Environment.GetEnvironmentVariable("ZHIPUAI_API_KEY");
var model = new OpenAIModel("glm-4-plus", key,
    "https://open.bigmodel.cn/api/paas/v4");

string response = await model.GenerateAsync(
    new ModelRequest
    {
        Messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("What is GLM?").Build()
        }
    }).Result.Text;
```

## Agent Integration

```csharp
using AgentScope.Core.Model.OpenAI;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new OpenAIModel("glm-4-plus", key,
        "https://open.bigmodel.cn/api/paas/v4"))
    .Build();
```

## Streaming

`OpenAIModel` implements `IStreamingChatModel`, and the GLM endpoint supports it:

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## Differences from Dedicated Models

- AgentScope has no `GLMModel` class; all functionality comes from `OpenAIModel`.
- Suggested environment variable: `ZHIPUAI_API_KEY` or `GLM_API_KEY`.
- Custom formatters can be passed via `OpenAIModel`'s `formatter` parameter.
- For GLM-specific parameters (e.g. thinking mode), pass `GenerateOptions` via the `defaultOptions` parameter.

See [Model](../../docs/building-blocks/model.md) for the full interface reference.
