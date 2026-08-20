# Gemini Model

`AgentScope.Core.Model.Gemini.GeminiModel` connects to Google Gemini models.

## Constructor

```csharp
GeminiModel(string modelName = "gemini-pro", string? apiKey = null,
    string? baseUrl = null, GenerateOptions? defaultOptions = null)
```

## Minimal Example

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.Gemini;

string key = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var model = new GeminiModel("gemini-2.0-flash", key);

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
using AgentScope.Core.Model.Gemini;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new GeminiModel("gemini-2.0-flash", key))
    .Build();
```

## Streaming

`GeminiModel` does **not** implement `IStreamingChatModel`. Use `GenerateAsync` for the full response.

## Notes

- `modelName` defaults to `"gemini-pro"`.
- When `apiKey` is omitted, read from the `GEMINI_API_KEY` environment variable.
- `GeminiModel` has no formatter parameter — the constructor is simpler than OpenAI's.
- Custom `GenerateOptions` can be passed via the constructor.

See [Model](../../docs/building-blocks/model.md) for the full interface reference.
