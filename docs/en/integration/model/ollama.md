# Ollama Model

`AgentScope.Core.Model.Ollama.OllamaModel` connects to a local Ollama service, extending `OpenAIModel`.

## Constructor

```csharp
OllamaModel(string modelName = "llama2", string? baseUrl = null)
```

When `baseUrl` is omitted, auto-detects the local Ollama instance (default `http://localhost:11434`).

## Minimal Example

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.Ollama;

var model = new OllamaModel("llama3");

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
using AgentScope.Core.Model.Ollama;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new OllamaModel("llama3"))
    .Build();
```

## Streaming

`OllamaModel` extends `OpenAIModel` and implements `IStreamingChatModel`:

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## Notes

- Ensure Ollama is installed and running locally, and the target model has been pulled (`ollama pull llama3`).
- `OllamaModel` constructor only exposes `modelName` and `baseUrl`. For custom formatters or generation options, use `new OpenAIModel(modelName, baseUrl: yourUrl)`.
- The default model name is `"llama2"`; specifying it explicitly is recommended.

See [Model](../../docs/building-blocks/model.md) for the full interface reference.
