# Anthropic Model

`AgentScope.Core.Model.Anthropic.AnthropicModel` connects to Anthropic Claude models.

## Constructor

```csharp
AnthropicModel(string modelName, string? apiKey = null, string? baseUrl = null,
    AnthropicChatFormatter? formatter = null, GenerateOptions? defaultOptions = null)
```

Constants: `DefaultBaseUrl = "https://api.anthropic.com"`, `MessagesEndpoint = "/v1/messages"`.

## Minimal Example

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.Anthropic;

string key = System.Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
var model = new AnthropicModel("claude-sonnet-4-20250514", key);

string response = await model.GenerateAsync(
    new ModelRequest
    {
        Messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("What is AgentScope?").Build()
        }
    }).Result.Text;
```

## Agent Integration

```csharp
using AgentScope.Core.Model.Anthropic;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new AnthropicModel("claude-sonnet-4-20250514", key))
    .Build();
```

## Streaming

`AnthropicModel` implements `IStreamingChatModel`:

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## Notes

- When `apiKey` is omitted, read from the `ANTHROPIC_API_KEY` environment variable.
- The endpoint uses the Messages API (`/v1/messages`), which differs from OpenAI's Chat Completions format.
- Custom `AnthropicChatFormatter` or `GenerateOptions` can be passed via the constructor.

See [Model](../../docs/building-blocks/model.md) for the full interface reference.
