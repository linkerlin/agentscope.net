---
title: "Model"
description: "Configure and connect LLM model providers in AgentScope .NET"
---

## Overview

The model layer separates shared contracts from provider implementations. `AgentScope.Core` keeps the common APIs (`IModel`, `ChatModelBase`, `IFormatter`, `ModelRegistry`, and the `IModelProvider` SPI). OpenAI, DashScope, Gemini, Anthropic, and Ollama implementations live in their own model extension NuGet packages.

At runtime, the model layer is two-tiered: at the top sit **Credentials** (based on `AgentScope.Core.Credential`), which carry a provider's API auth fields; below them sit **Chat Models**, the concrete inference implementations attached to a credential.

```text
CredentialBase/
└── ChatModelBase/
    ├── OpenAIChatModel
    ├── AnthropicChatModel
    ├── DashScopeChatModel
    ├── GeminiChatModel
    └── OllamaChatModel
```

A **Credential** carries a provider's API auth fields (`ApiKey`, `BaseUrl`, …). Starting from a credential, you can call `ListModels()` to enumerate the models available under that provider (returns `Task<List<ModelCard>>`).

This layering matches the natural UX in a frontend — register the credential first, then pick a model under it — so the UI authenticates once and shows everything that provider supports.

## Model extension packages

Provider-specific model implementations have been moved out of `AgentScope.Core` into independent extension NuGet packages. Each provider package owns its chat model, credential, formatter, DTO, exception, and SDK/API client, etc.

| Provider | NuGet package | Main namespace |
|----------|---------------|----------------|
| OpenAI | `AgentScope.Extensions.Model.OpenAI` | `AgentScope.Extensions.Model.OpenAI` |
| DashScope | `AgentScope.Extensions.Model.DashScope` | `AgentScope.Extensions.Model.DashScope` |
| Gemini | `AgentScope.Extensions.Model.Gemini` | `AgentScope.Extensions.Model.Gemini` |
| Anthropic | `AgentScope.Extensions.Model.Anthropic` | `AgentScope.Extensions.Model.Anthropic` |
| Ollama | `AgentScope.Extensions.Model.Ollama` | `AgentScope.Extensions.Model.Ollama` |

### Migration checklist

1. Add the provider extension package. For example, DashScope:

```xml
<PackageReference Include="AgentScope.Extensions.Model.DashScope" Version="2.0.0" />
```

Other provider artifacts follow the same pattern: `AgentScope.Extensions.Model.OpenAI`, `AgentScope.Extensions.Model.Gemini`, `AgentScope.Extensions.Model.Anthropic`, and `AgentScope.Extensions.Model.Ollama`.

2. Replace provider imports from `AgentScope.Core.Model.*` with `AgentScope.Extensions.Model.<Provider>.*`.
3. Replace provider formatter imports from `AgentScope.Core.Formatter.<Provider>.*` with `AgentScope.Extensions.Model.<Provider>.Formatter.*`.
4. For ASP.NET Core applications, replace the generic model creation path with the matching provider-specific package and its `AgentScope.<Provider>` configuration sections.

```xml
<PackageReference Include="AgentScope.DashScope.AspNetCore" Version="2.0.0" />
```

## Choose a creation path

### String model id

For simple non-ASP.NET Core applications, use a `ModelRegistry` string id such as `dashscope:qwen-plus`, `openai:gpt-4.1-mini`, or `deepseek:deepseek-v4-flash`. Add the matching model extension NuGet package, set the provider's standard environment variable such as `DASHSCOPE_API_KEY`, `OPENAI_API_KEY`, or `DEEPSEEK_API_KEY`, and pass the id directly to the agent:

```csharp
ReActAgent agent =
        ReActAgent.Builder()
                .Name("assistant")
                .Model("dashscope:qwen-plus") // resolved internally by ModelRegistry.Resolve(modelId)
                .Build();
```

The extension package is discovered through .NET's reflection/type-loading mechanism. The model provider reads its standard environment variables such as `DASHSCOPE_API_KEY`, `OPENAI_API_KEY`, `DEEPSEEK_API_KEY`, `GLM_API_KEY`, `ANTHROPIC_API_KEY`, or `GEMINI_API_KEY`. Ollama reads `OLLAMA_BASE_URL` when present and otherwise defaults to the local Ollama endpoint.

### Explicit model builder

When you need a custom API key, base URL, formatter, transport, timeout, generation options, or other provider-specific configuration, build the model explicitly and pass the `IModel` instance to the agent:

```csharp
using AgentScope.Extensions.Model.DashScope;
using AgentScope.Extensions.Model.DashScope.Formatter;

DashScopeChatModel model =
        DashScopeChatModel.Builder()
                .ApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
                .ModelName("qwen-plus")
                .Stream(true)
                .Formatter(new DashScopeChatFormatter())
                .Build();

ReActAgent agent =
        ReActAgent.Builder()
                .Name("assistant")
                .Model(model)
                .Build();
```

### ASP.NET Core applications

For ASP.NET Core, prefer provider-specific packages such as `AgentScope.OpenAI.AspNetCore`, `AgentScope.DashScope.AspNetCore`, `AgentScope.Gemini.AspNetCore`, `AgentScope.Anthropic.AspNetCore`, and `AgentScope.Ollama.AspNetCore`. These packages directly depend on the matching model extension, create DI-managed `IModel` instances, and leave the generic package focused on common AgentScope infrastructure. They do not create models through the static `ModelRegistry`; advanced users can always provide their own `IModel` registration.

OpenAI example:

```yaml
AgentScope:
  Model:
    Provider: openai
  OpenAI:
    ApiKey: ${OPENAI_API_KEY}
    ModelName: gpt-4.1-mini
    Stream: true
```

#### Builder customizers

Provider-specific packages also expose ordered `IOrderedFilter` / `IConfigureOptions` style customizers for the
auto-configured chat model builders. Use them when configuration binding covers the common
settings but you still need to tune builder-only options such as custom formatters,
default generation options, proxy/client settings, or provider-specific flags.

| Package | Customizer type |
|---------|-----------------|
| `AgentScope.OpenAI.AspNetCore` | `IOpenAIChatModelBuilderCustomizer` |
| `AgentScope.DashScope.AspNetCore` | `IDashScopeChatModelBuilderCustomizer` |
| `AgentScope.Gemini.AspNetCore` | `IGeminiChatModelBuilderCustomizer` |
| `AgentScope.Anthropic.AspNetCore` | `IAnthropicChatModelBuilderCustomizer` |
| `AgentScope.Ollama.AspNetCore` | `IOllamaChatModelBuilderCustomizer` |

Customizer registrations are applied after configuration properties are bound and before
`builder.Build()` is called. Multiple customizers are supported and follow `IOrderedFilter`
ordering.

```csharp
using AgentScope.Core.Model;
using AgentScope.OpenAI.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

public static class ModelCustomizerRegistration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureOpenAIModelBuilder(builder =>
                builder.DefaultOptions(
                        GenerateOptions.Builder()
                                .Temperature(0.2)
                                .ParallelToolCalls(false)
                                .Build()));
    }
}
```

## ModelRegistry and ModelCreationContext

`ModelRegistry` is a global registry for model instance creation and lookup, supporting multiple resolution strategies. During resolution, it tries in priority order: named model instances directly registered via `ModelRegistry.Register(name, model)`, custom factories registered via `RegisterFactory(regex, factory)`, and `IModelProvider` implementations automatically discovered from extension packages through .NET reflection.

For simple scenarios, prefer a string id in the `provider:model` format together with the provider's standard environment variable; for fine-grained control, use explicit model builders. `ModelCreationContext` is mainly for integration-layer code that must resolve models dynamically.

### Advanced integration context

`ModelCreationContext` is for integration layers that must create models dynamically without importing a concrete provider builder, such as multi-tenant gateways, plugin systems, or framework adapters. It can pass common values such as API key, base URL, endpoint path, stream mode, and extension-defined options/components to the provider:

```csharp
using AgentScope.Core.Model;

ModelCreationContext context =
        ModelCreationContext.Builder()
                .ApiKey(tenantApiKey)
                .BaseUrl(tenantBaseUrl)
                .Stream(false)
                // Extension-defined scalar options, keyed by names the provider documents.
                .Option("contextWindowSize", 128000)
                // Type-keyed components for richer provider settings, transports, or formatters.
                .Component(
                        typeof(GenerateOptions),
                        GenerateOptions.Builder()
                                .ParallelToolCalls(false)
                                .Build())
                .Build();

IModel model = ModelRegistry.Resolve("openai:gpt-4.1-mini", context);
```

### Cache policy

`ModelRegistry` caches models resolved from simple `provider:model` strings. Context-aware creation is not cached by default to avoid reusing a model instance with a different tenant's API key, base URL, or stream setting.

| Policy | Behavior |
|--------|----------|
| `DEFAULT` | `Resolve(string)` keeps legacy model-id caching. `Resolve(string, nonEmptyContext)` is not cached. |
| `DISABLED` | Never cache; every resolution creates a new model instance. |
| `ENABLED` | Cache only when the caller explicitly opts in. Use `CacheId(...)` for tenant- or configuration-specific identity. |

If `CachePolicy.ENABLED` is used with `Option(...)` or `Component(...)`, the user must provide a `CacheId`.

### IModelProvider SPI

Provider extension packages are discovered through .NET reflection by scanning for `IModelProvider` implementations. A provider can implement `Supports(string, ModelCreationContext)` and `Create(string, ModelCreationContext)` to consume context values. Simple providers can keep implementing the original `Supports(string)` and `Create(string)` methods because the context-aware methods have compatible defaults.

## Chat model

A **Chat Model** is the LLM driving conversation and tool calling, with input and output potentially spanning multiple modalities. AgentScope .NET currently ships:

| Provider | Class | Notes |
|----------|-------|-------|
| OpenAI | `OpenAIChatModel` | Chat Completions API; works with vLLM and OpenAI-compatible endpoints (DeepSeek, Kimi, …) |
| Anthropic | `AnthropicChatModel` | Claude models; prompt caching and thinking |
| DashScope | `DashScopeChatModel` | Qwen models; multi-modal (vision/audio/video), reasoning |
| Gemini | `GeminiChatModel` | Google Gemini; multi-modal |
| Ollama | `OllamaChatModel` | Locally hosted LLMs; credential optional |

Provider credential classes live with their model extension packages, for example `OpenAICredential`, `AnthropicCredential`, `DashScopeCredential`, `GeminiCredential`, and `OllamaCredential`. OpenAI-compatible credentials such as `DeepSeekCredential`, `KimiCredential`, and `XAICredential` remain available from core.

### Creating a chat model

Each chat model is built with a builder. The most common fields are `ApiKey`, `ModelName`, `Stream`, `Formatter`, `DefaultOptions`. Three typical setups:

::::{tab-set}
:::{tab-item} Streaming
```csharp
using AgentScope.Extensions.Model.DashScope;
using AgentScope.Extensions.Model.DashScope.Formatter;

DashScopeChatModel model =
        DashScopeChatModel.Builder()
                .ApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
                .ModelName("qwen-plus")
                .Stream(true)
                .Formatter(new DashScopeChatFormatter())
                .Build();
```
:::
:::{tab-item} Tools
```csharp
using AgentScope.Extensions.Model.DashScope;
using AgentScope.Extensions.Model.DashScope.Formatter;
using AgentScope.Core.Model;

DashScopeChatModel model =
        DashScopeChatModel.Builder()
                .ApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
                .ModelName("qwen-plus")
                .Stream(false)
                .Formatter(new DashScopeChatFormatter())
                .DefaultOptions(
                        GenerateOptions.Builder()
                                .ParallelToolCalls(false)
                                .Build())
                .Build();
```
:::
:::{tab-item} Reasoning
```csharp
using AgentScope.Extensions.Model.DashScope;
using AgentScope.Extensions.Model.DashScope.Formatter;
using AgentScope.Core.Model;

DashScopeChatModel model =
        DashScopeChatModel.Builder()
                .ApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
                .ModelName("qwen3-235b-a22b-thinking-2507")
                .Stream(true)
                .EnableThinking(true)
                .Formatter(new DashScopeChatFormatter())
                .DefaultOptions(
                        GenerateOptions.Builder()
                                .ThinkingBudget(2048)
                                .Build())
                .Build();
```
:::
::::

Common builder fields:

| Field | Type | Description |
|-------|------|-------------|
| `ApiKey` | `string` | API key (some providers also accept `Credential(...)`) |
| `ModelName` | `string` | Model identifier (e.g. `"qwen-plus"`) |
| `Stream` | `bool` | Whether to stream output |
| `DefaultOptions` | `GenerateOptions` | Provider-specific options (`Temperature`, `MaxTokens`, `ThinkingBudget`, `ParallelToolCalls`, …) |
| `Formatter` | `IFormatter` | Override the default message formatter |
| `BaseUrl` | `string` | Custom service endpoint (e.g. an OpenAI-compatible proxy) |

### Calling a chat model

The `IModel` interface exposes a unified `StreamAsync(messages, tools, options)` returning `IAsyncEnumerable<ChatResponse>`:

```csharp
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Extensions.Model.DashScope;
using AgentScope.Extensions.Model.DashScope.Formatter;
using System;
using System.Collections.Generic;

DashScopeChatModel model =
        DashScopeChatModel.Builder()
                .ApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
                .ModelName("qwen-plus")
                .Stream(true)
                .Formatter(new DashScopeChatFormatter())
                .Build();

await foreach (var chunk in model.StreamAsync(
        new List<Msg> { new UserMessage("Count from 1 to 5.") },
        /* tools = */ new List<ToolSchema>(),
        GenerateOptions.Builder().Build()))
{
    Console.WriteLine("Chunk: " + chunk.GetContent());
}
Console.WriteLine("Stream completed");
```

A `ChatResponse` carries a list of content blocks (`TextBlock`, `ThinkingBlock`, `ToolUseBlock`, `DataBlock`) and a `ChatUsage` recording token counts and timing.

In practice you usually call models indirectly via `ReActAgent`. For lightweight direct invocation, see `agentscope-examples/documentation/.../model/ModelRegistryExample.cs`.

### Generating structured output

The agent layer offers a convenience overload for binding the model output to a POCO via `ReActAgent.CallAsync(msgs, structuredOutputType, runtimeContext)`:

```csharp
using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using System;
using System.Collections.Generic;

public class WeatherInfo
{
    public string City { get; set; }
    public double Temperature { get; set; }
    public string Unit { get; set; }
}

Msg msg =
        await agent.CallAsync(
                new List<Msg> { new UserMessage("What's the weather in Shanghai?") },
                typeof(WeatherInfo),
                RuntimeContext.Empty);

WeatherInfo info = msg.GetStructuredData<WeatherInfo>();
```

How it works: the framework synthesizes a forced structured tool call from the target class, validates and repairs the model output, and writes the result into `Msg.Metadata` under the `structured_output` key, so `GetStructuredData<T>()` can deserialize it directly. Complete example: `agentscope-examples/documentation/.../structuredoutput/StructuredOutputExample.cs`.

#### Structured output path selection

The framework provides two structured output paths:

| Path | Condition | Mechanism |
|------|-----------|-----------|
| **Native** | `SupportsNativeStructuredOutput() = true` | Uses `response_format` + `json_schema` for direct JSON output |
| **Fallback** (default) | `SupportsNativeStructuredOutput() = false` | Injects a `generate_response` synthetic tool; model returns structured data via tool call |

If the native path fails (e.g. model returns HTTP 400), the framework **automatically falls back** to the synthetic tool path — no user intervention needed.

#### Default behavior per provider

| Provider | `SupportsNativeStructuredOutput` | Notes |
|----------|----------------------------------|-------|
| OpenAI (GPT-4o, etc.) | `true` | Native `json_schema` support |
| OpenAI (DeepSeek/GLM formatter) | `false` | Not supported; auto-fallback |
| DashScope | `false` | Native endpoint only supports `json_object`, not `json_schema`; fallback by default |
| Anthropic | `false` (default) | — |

> **DashScope users**: Thinking mode (`EnableThinking(true)`) does not support structured output at all — the framework forces the fallback path.

#### Explicit configuration

If you confirm your model/endpoint supports `json_schema`, enable the native path via builder:

```csharp
DashScopeChatModel model = DashScopeChatModel.Builder()
        .ApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
        .ModelName("qwen-plus")
        .NativeStructuredOutput(true)  // explicitly enable native json_schema path
        .Build();
```

#### Structured output with tool calling

When an agent has both tools and structured output, some OpenAI-compatible providers (e.g. Kimi, Deepseek) prioritise the `response_format` constraint and skip tool calling entirely. Set `NativeStructuredOutputWithTools(false)` to resolve this:

```csharp
OpenAIChatModel model = OpenAIChatModel.Builder()
        .ApiKey("...")
        .BaseUrl("https://api.moonshot.cn/v1")
        .ModelName("moonshot-v1-8k")
        .NativeStructuredOutputWithTools(false)
        .Build();
```

`DashScopeChatModel` supports this option as well. For native OpenAI models (GPT-4o, etc.) the default behavior handles both correctly — no configuration needed.

### Formatter

A **Formatter** converts AgentScope `Msg` objects into the request payload each provider's API expects. It is configured via the chat model builder's `Formatter(...)`. Each provider ships two formatters:

| Type | Use case |
|------|----------|
| **ChatFormatter** (default) | Standard single-agent chat. Each `Msg` maps 1:1 to one API message, preserving the role (`USER`, `ASSISTANT`, `SYSTEM`). |
| **MultiAgentFormatter** | Multi-agent scenarios such as debate or moderator setups. Consecutive agent messages are aggregated and tagged with the sender's name. |

To switch to multi-agent mode, just pass the MultiAgent variant — no agent code changes:

```csharp
using AgentScope.Extensions.Model.DashScope;
using AgentScope.Extensions.Model.DashScope.Formatter;

DashScopeChatModel model =
        DashScopeChatModel.Builder()
                .ApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
                .ModelName("qwen-plus")
                .Stream(true)
                .Formatter(new DashScopeMultiAgentFormatter())
                .Build();
```

Per-provider formatters now live with their provider extension packages:

| Provider | Chat | MultiAgent |
|----------|------|------------|
| DashScope | `DashScopeChatFormatter` | `DashScopeMultiAgentFormatter` |
| OpenAI | `OpenAIChatFormatter` | `OpenAIMultiAgentFormatter` |
| Anthropic | `AnthropicChatFormatter` | `AnthropicMultiAgentFormatter` |
| Gemini | `GeminiChatFormatter` | `GeminiMultiAgentFormatter` |
| Ollama | `OllamaChatFormatter` | `OllamaMultiAgentFormatter` |

If your provider's payload doesn't fit any of these, implement the `IFormatter<TReq, TResp, TParams>` interface (`AgentScope.Core.Formatter`) and pass it through the same `Formatter(...)` builder.

### Custom provider

The minimal path to a new provider: implement a `CredentialBase` subclass and a `ChatModelBase` subclass.

#### Step 1: Define the credential

Extend `CredentialBase` and implement `GetChatModelType()`:

```csharp
using AgentScope.Core.Credential;
using AgentScope.Core.Model;

public class MyProviderCredential : CredentialBase
{
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public MyProviderCredential(string apiKey, string baseUrl)
        : base("my_provider:" + apiKey[..Math.Min(4, apiKey.Length)])
    {
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://api.myprovider.com/v1";
    }

    public string GetApiKey() => _apiKey;
    public string GetBaseUrl() => _baseUrl;

    public override Type GetChatModelType() => typeof(MyProviderChatModel);
}
```

#### Step 2: Implement the chat model

Extend `ChatModelBase` and implement `DoStreamAsync`:

```csharp
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using System;
using System.Collections.Generic;

public class MyProviderChatModel : ChatModelBase
{
    private readonly MyProviderCredential _credential;
    private readonly string _modelName;

    public MyProviderChatModel(MyProviderCredential credential, string modelName)
    {
        _credential = credential;
        _modelName = modelName;
    }

    protected override IAsyncEnumerable<ChatResponse> DoStreamAsync(
            List<Msg> messages, List<ToolSchema> tools, GenerateOptions options)
    {
        // Call the provider's API, wrap responses into an IAsyncEnumerable<ChatResponse>.
        return System.Linq.AsyncEnumerable.Empty<ChatResponse>();
    }
}
```

#### Step 3: Register with the ModelRegistry (optional)

`ModelRegistry` lets `ReActAgent.Builder().Model("provider:model-name")` resolve models from a string:

```csharp
using AgentScope.Core.Model;

ModelRegistry.RegisterFactory(
        "myprov:.*",
        modelId => new MyProviderChatModel(
                new MyProviderCredential(Environment.GetEnvironmentVariable("MYPROV_API_KEY"), null),
                modelId["myprov:".Length..]));

// Then:
// ReActAgent.Builder().Model("myprov:my-model-v1")...
```

## Frontend integration

### What is ModelCard

`ModelCard` (`Credential/ModelCard.cs`) is a declarative description of a model's capabilities and constraints. It powers frontends — the model picker, parameter form, and capability toggles can render dynamically against it without hard-coding any provider-specific logic.

Today, `ModelCard` is a minimal record:

| Method | Type | Description |
|--------|------|-------------|
| `ModelName` | `string` | Model identifier (e.g. `"claude-sonnet-4-6"`) |
| `DisplayName` | `string` | Human-readable label (e.g. `"Claude Sonnet 4.6"`) |
| `ContextSize` | `int` | Maximum context window (in tokens) |

:::{note}
The `ModelCard` schema is intentionally minimal at this stage; capability flags (input/output MIME types) and parameter schemas will be added as model-discovery infrastructure matures.
:::

### Fetching ModelCards

Call `CredentialBase.ListModels()`, returning `Task<List<ModelCard>>`:

```csharp
using AgentScope.Core.Credential;
using AgentScope.Extensions.Model.Anthropic.Credential;
using System;
using System.Collections.Generic;

AnthropicCredential cred = new AnthropicCredential(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
List<ModelCard> cards = await cred.ListModels();

foreach (ModelCard card in cards)
{
    Console.WriteLine(card.ModelName + ": context=" + card.ContextSize);
}
```

`GetChatModelType()` returns the matching `ChatModelBase` subclass — useful for reflectively building a default model:

```csharp
Type modelType = cred.GetChatModelType();
```

This design lets frontends discover every model available under a provider with just one credential — no hard-coded provider logic.
