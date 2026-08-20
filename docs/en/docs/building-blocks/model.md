---
title: "Model"
description: "IModel model system: built-in providers, streaming interfaces, ModelFactory"
---

## Overview

All models are in the **`AgentScope.Core`** package (no separate model extension packages), implementing unified interfaces:

```csharp
public interface IModel
{
    string ModelName { get; }
    IObservable<ModelResponse> Generate(ModelRequest request);    // Reactive (System.Reactive)
    Task<ModelResponse> GenerateAsync(ModelRequest request);      // Task async (primary method)
}

public interface IStreamingChatModel
{
    IAsyncEnumerable<ChatResponse> GenerateStreamAsync(
        List<Msg> messages, CancellationToken cancellationToken = default);
}
```

- `ModelRequest`: `Messages` (`List<Msg>`) + optional `Options` (`Dictionary<string, object>`, e.g., temperature).
- `ModelResponse`: `Text` / `Metadata` / `Success` / `Error`.
- `ChatResponse` inherits `ModelResponse`, additionally contains `Id` / `Content` / `ToolCalls` (`List<ToolCallInfo>`) / `Usage` (`ChatUsage`: InputTokens, OutputTokens, TotalTokens, TimeSeconds) / `Model` / `StopReason` / `IsComplete`.

`EnhancedReActAgent`'s streaming loop automatically detects whether the model implements `IStreamingChatModel`; if so, it produces `ReasoningChunk` events per chunk, otherwise returns the entire response at once.

## Built-in Models

| Class | Namespace | Constructor Signature | Streaming |
|-------|-----------|-----------------------|-----------|
| `OpenAIModel` | `AgentScope.Core.Model.OpenAI` | `OpenAIModel(string modelName, string? apiKey = null, string? baseUrl = null, OpenAIClient? client = null, OpenAIChatFormatter? formatter = null, GenerateOptions? defaultOptions = null)` | ✅ (two overloads, can pass `GenerateOptions`) |
| `DashScopeModel` | `AgentScope.Core.Model.DashScope` | `DashScopeModel(string modelName, string? apiKey = null, string? baseUrl = null, DashScopeChatFormatter? formatter = null, GenerateOptions? defaultOptions = null)` | ✅ |
| `AnthropicModel` | `AgentScope.Core.Model.Anthropic` | `AnthropicModel(string modelName, string? apiKey = null, string? baseUrl = null, AnthropicChatFormatter? formatter = null, GenerateOptions? defaultOptions = null)` | ✅ |
| `GeminiModel` | `AgentScope.Core.Model.Gemini` | `GeminiModel(string modelName = "gemini-pro", string? apiKey = null, string? baseUrl = null, GenerateOptions? defaultOptions = null)` | ❌ (does not implement IStreamingChatModel) |
| `DeepSeekModel` | `AgentScope.Core.Model.DeepSeek` | `DeepSeekModel(string modelName = "deepseek-chat", string? apiKey = null)` — apiKey defaults to `DEEPSEEK_API_KEY` environment variable | ✅ (inherits OpenAIModel) |
| `OllamaModel` | `AgentScope.Core.Model.Ollama` | `OllamaModel(string modelName = "llama2", string? baseUrl = null)` — baseUrl defaults to local Ollama detection | ✅ (inherits OpenAIModel) |
| `MockModel` | `AgentScope.Core.Model` | `MockModel(string modelName = "mock-model")` or `MockModel.Builder().ModelName("mock").Build()` | ✅ (echoes input) |

### Usage Examples

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.DashScope;

// DashScope (Tongyi Qianwen)
IModel dashscope = new DashScopeModel("qwen-plus", Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"));

// OpenAI-compatible service (custom baseUrl)
IModel proxy = new OpenAIModel("gpt-4o", apiKey, "https://your-proxy.example.com/v1");

// DeepSeek / Ollama: minimal construction
IModel deepseek = new DeepSeekModel();                     // deepseek-chat + environment variable
IModel local    = new OllamaModel("qwen3:8b");             // Local Ollama

// Test double
IModel mock = MockModel.Builder().ModelName("mock-model").Build();
```

### Direct Model Call

```csharp
var request = new ModelRequest
{
    Messages = [Msg.Builder().Role("user").TextContent("Hello").Build()],
    Options = new Dictionary<string, object> { ["temperature"] = 0.7 }
};

ModelResponse resp = await model.GenerateAsync(request);
Console.WriteLine($"{resp.Success}: {resp.Text}");

// Streaming
await foreach (ChatResponse chunk in ((IStreamingChatModel)model).GenerateStreamAsync(request.Messages))
{
    Console.Write(chunk.Content);
}
```

## ModelFactory

`AgentScope.Core.ModelFactory` is a static utility class that creates models by provider string:

```csharp
// Signature: Create(string provider, string modelName, string apiKey, string? baseUrl = null)
IModel model = ModelFactory.Create("dashscope", "qwen-plus", apiKey);

// Or from a dictionary configuration
IModel model2 = ModelFactory.Create(new Dictionary<string, string>
{
    ["provider"] = "openai",
    ["model"] = "gpt-4o",
    ["apiKey"] = apiKey,
    ["baseUrl"] = "https://api.openai.com/v1"      // Optional
});
```

Supported providers: `openai` / `azure` / `anthropic` / `deepseek` / `gemini` / `dashscope` / `ollama`. Extension methods `ModelFactoryExtensions.IsSupportedProvider(provider)` / `GetDefaultModel(provider)` / `GetSupportedProviders()` are available for querying.

## Formatter System

Message formatters for each provider are located in `AgentScope.Core.Formatter.*`:

| Formatter | Namespace | Construction |
|-----------|-----------|-------------|
| `OpenAIChatFormatter` | `AgentScope.Core.Formatter.OpenAI` | `(string modelName)` |
| `DashScopeChatFormatter` | `AgentScope.Core.Formatter.DashScope` | `()` (default qwen-plus) or `(string modelName)` |
| `AnthropicChatFormatter` | `AgentScope.Core.Formatter.Anthropic` | `()` (default claude-3-5-sonnet) or `(string modelName)` |
| `GeminiFormatter` | `AgentScope.Core.Formatter.Gemini` | `(GenerateOptions? defaultOptions = null)` |

Models automatically create a default Formatter during construction; inject a custom formatter via the constructor parameter when custom formatting behavior is needed (except GeminiModel, which is configured via `GenerateOptions`).

:::{note}
`OpenAIModel`, `DashScopeModel`, `AnthropicModel`, `GeminiModel` all inherit the abstract base class `ModelBase` (providing read-only `ModelName` storage); `DeepSeekModel` and `OllamaModel` directly inherit `OpenAIModel`.
:::

## Integration with Agent

```csharp
EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(new DashScopeModel("qwen-plus", apiKey))    // Required
    .Build();

// HarnessAgent accepts model via HarnessAgentBuilder.WithModel(IModel)
HarnessAgent harness = new HarnessAgentBuilder()
    .WithModel(new OpenAIModel("gpt-4o", apiKey))
    .Build();
```

## Related Documentation

- [Agent](./agent.md)
- [Model Integration Quick Reference](../../integration/model/index.md)
