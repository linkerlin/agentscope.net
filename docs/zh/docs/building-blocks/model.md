---
title: "Model"
description: "在 AgentScope .NET 中配置并连接 LLM 模型提供商"
---

## 概述

模型层把共享契约和具体模型提供商实现分开。`AgentScope.Core` 只保留通用 API（`Model`、`ChatModelBase`、`Formatter`、`ModelRegistry` 和 `ModelProvider` SPI）；OpenAI、DashScope、Gemini、Anthropic、Ollama 的具体实现分别位于各自的模型扩展模块中。

运行时模型层采用两层结构：上层是 **Credential**（基于 `AgentScope.Core.Credential` 中的通用基类），承载某个提供商的 API 鉴权字段；下层是 **Chat Model**，即在该凭证基础上对接的具体推理模型实现。

```text
CredentialBase/
└── ChatModelBase/
    ├── OpenAIChatModel
    ├── AnthropicChatModel
    ├── DashScopeChatModel
    ├── GeminiChatModel
    └── OllamaChatModel
```

**Credential** 承载某个提供商的 API 认证字段（`ApiKey`、`BaseUrl` 等）。从一个凭证出发，可以通过 `ListModels()` 获取该提供商支持的模型列表（`List<ModelCard>`）。

这种分层与前端的自然交互流程一致 —— 先注册凭证，再从凭证下挑选模型 —— 让界面只需鉴权一次，就能展示该提供商支持的所有模型。

## 模型扩展模块

特定模型提供商的实现已经从 `AgentScope.Core` 迁移到独立扩展模块中。每个模型适配模块自己维护 chat model、credential、formatter、DTO、异常、SDK/API client 等。

| 提供商 | NuGet package | 主要命名空间 |
|--------|---------------|-------------|
| OpenAI | `AgentScope.Extensions.Model.OpenAI` | `AgentScope.Extensions.Model.OpenAI` |
| DashScope | `AgentScope.Extensions.Model.DashScope` | `AgentScope.Extensions.Model.DashScope` |
| Gemini | `AgentScope.Extensions.Model.Gemini` | `AgentScope.Extensions.Model.Gemini` |
| Anthropic | `AgentScope.Extensions.Model.Anthropic` | `AgentScope.Extensions.Model.Anthropic` |
| Ollama | `AgentScope.Extensions.Model.Ollama` | `AgentScope.Extensions.Model.Ollama` |

### 迁移步骤

1. 增加对应模型提供商扩展模块依赖。以 DashScope 为例：

```xml
<PackageReference Include="AgentScope.Extensions.Model.DashScope" />
```

其他模型扩展 package 遵循同样模式：`AgentScope.Extensions.Model.OpenAI`、`AgentScope.Extensions.Model.Gemini`、`AgentScope.Extensions.Model.Anthropic`、`AgentScope.Extensions.Model.Ollama`。

2. 将模型提供商实现的 using 从 `AgentScope.Core.Model.*` 改为 `AgentScope.Extensions.Model.<provider>.*`。
3. 将模型提供商 formatter using 从 `AgentScope.Core.Formatter.<provider>.*` 改为 `AgentScope.Extensions.Model.<provider>.Formatter.*`。
4. ASP.NET Core 应用中，改用对应提供商 integration 和 `AgentScope.<provider>.*` 配置。

## 选择模型创建方式

### 字符串 model id

简单的非 ASP.NET Core 应用可以使用 `dashscope:qwen-plus`、`openai:gpt-4.1-mini`、`deepseek:deepseek-v4-flash` 这样的字符串 id。引入对应模型扩展模块，设置模型提供商的标准环境变量，例如 `DASHSCOPE_API_KEY`、`OPENAI_API_KEY` 或 `DEEPSEEK_API_KEY`，然后直接把 id 传给 agent：

```csharp
ReActAgent agent =
        ReActAgent.Builder()
                .WithName("assistant")
                .WithModel("dashscope:qwen-plus") // 底层由 ModelRegistry.Resolve(modelId) 解析
                .Build();
```

扩展模块会通过 .NET 的 `IModelProvider` 约定被自动发现。模型提供商会读取自己的标准环境变量，例如 `DASHSCOPE_API_KEY`、`OPENAI_API_KEY`、`GLM_API_KEY`、`DEEPSEEK_API_KEY`、`ANTHROPIC_API_KEY`、`GEMINI_API_KEY`。Ollama 会在存在时读取 `OLLAMA_BASE_URL`，否则默认使用本地 Ollama endpoint。

### 显式 Model builder

需要自定义 API key、base URL、formatter、transport、timeout、生成参数或其他提供商专属配置时，推荐显式构造模型，再把 `Model` 实例传给 agent：

```csharp
using AgentScope.Extensions.Model.DashScope;
using AgentScope.Extensions.Model.DashScope.Formatter;

DashScopeChatModel model =
        DashScopeChatModel.Builder()
                .WithApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
                .WithModelName("qwen-plus")
                .WithStream(true)
                .WithFormatter(new DashScopeChatFormatter())
                .Build();

ReActAgent agent =
        ReActAgent.Builder()
                .WithName("assistant")
                .WithModel(model)
                .Build();
```

### ASP.NET Core 应用

ASP.NET Core 场景下，优先使用特定模型提供商的 integration，例如 `AgentScope.Extensions.Model.OpenAI`、`AgentScope.Extensions.Model.DashScope`、`AgentScope.Extensions.Model.Gemini`、`AgentScope.Extensions.Model.Anthropic`、`AgentScope.Extensions.Model.Ollama`。这些 integration 直接依赖对应模型扩展模块，通过 DI 注册 `Model` 实例，通用的 `AgentScope.Core` 继续负责 AgentScope 的公共基础设施。它们不会通过静态 `ModelRegistry` 创建模型；高级用户始终可以自定义 `Model` 注册。

OpenAI 示例：

```yaml
AgentScope:
  Model:
    Provider: openai
  OpenAI:
    ApiKey: "${OPENAI_API_KEY}"
    ModelName: "gpt-4.1-mini"
    Stream: true
```

#### Builder customizer

各模型提供商的 ASP.NET Core integration 还提供了有序的 builder customizer。它适合用于
`appsettings.json` 已覆盖常见配置、但仍需要设置 builder 专属能力的场景，例如自定义
formatter、默认生成参数、代理/client 配置，或其他提供商专属开关。

| Integration | Customizer 类型 |
|-------------|-----------------|
| `AgentScope.Extensions.Model.OpenAI` | `OpenAIChatModelBuilderCustomizer` |
| `AgentScope.Extensions.Model.DashScope` | `DashScopeChatModelBuilderCustomizer` |
| `AgentScope.Extensions.Model.Gemini` | `GeminiChatModelBuilderCustomizer` |
| `AgentScope.Extensions.Model.Anthropic` | `AnthropicChatModelBuilderCustomizer` |
| `AgentScope.Extensions.Model.Ollama` | `OllamaChatModelBuilderCustomizer` |

这些 customizer 会在 DI 绑定属性之后、调用 `builder.Build()` 之前执行。可以注册多个
customizer，并通过 .NET 的 `IOrdered` 或 `[Order]` 控制执行顺序。

```csharp
using AgentScope.Core.Model;
using AgentScope.Extensions.Model.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class ModelCustomizerConfiguration
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureOpenAIModel(builder =>
        {
            builder.WithDefaultOptions(
                    GenerateOptions.Builder()
                            .WithTemperature(0.2)
                            .WithParallelToolCalls(false)
                            .Build());
        });
    }
}
```

## ModelRegistry 与 ModelCreationContext

`ModelRegistry` 是一个用于模型实例创建与查找的全局注册中心，支持多种解析策略。解析时按优先级依次尝试：通过 `ModelRegistry.Register(name, model)` 直接注册的命名模型实例、通过 `RegisterFactory(regex, factory)` 注册的自定义工厂，以及通过约定自动发现的扩展模块提供的 `IModelProvider` 实现。

简单场景推荐使用 `provider:model` 格式的 id 和模型提供商的标准环境变量；需要精细控制时，优先使用显式的模型 Builder。`ModelCreationContext` 主要面向需要动态解析模型的集成层代码。

### 高级集成上下文

`ModelCreationContext` 面向需要动态创建模型、但不方便直接依赖具体提供商 builder 的集成层代码，例如多租户网关、插件系统或框架适配层。它可以把 API key、base URL、endpoint path、stream 模式，以及扩展模块定义的 options/components 传给 SP 提供商实现：

```csharp
using AgentScope.Core.Model;

ModelCreationContext context =
        ModelCreationContext.Builder()
                .WithApiKey(tenantApiKey)
                .WithBaseUrl(tenantBaseUrl)
                .WithStream(false)
                // 扩展模块定义的标量配置，key 由具体模型提供商文档约定。
                .WithOption("contextWindowSize", 128000)
                // 以类型为 key 的组件对象，用于传入更复杂的提供商配置、transport 或 formatter。
                .WithComponent(
                        typeof(GenerateOptions),
                        GenerateOptions.Builder()
                                .WithParallelToolCalls(false)
                                .Build())
                .Build();

Model model = ModelRegistry.Resolve("openai:gpt-4.1-mini", context);
```

### 缓存策略

`ModelRegistry` 会缓存简单 `provider:model` 解析出的模型。带 context（`ModelCreationContext`）解析出的模型默认不缓存，避免不同租户的 API key、base URL 或 stream 配置复用到同一个模型实例。

| 策略 | 行为 |
|------|------|
| `Default` | `Resolve(string)` 保持按 model id 缓存的旧行为；`Resolve(string, nonEmptyContext)` 默认不缓存。 |
| `Disabled` | 永不缓存，每次解析都会创建新的模型实例。 |
| `Enabled` | 显式开启缓存。建议用 `CacheId(...)` 表达租户或配置维度的身份。 |

如果 `CachePolicy.Enabled` 搭配 `WithOption(...)` 或 `WithComponent(...)` 使用，用户必须提供 `CacheId`。

### IModelProvider SPI

模型提供商扩展模块通过约定暴露 `IModelProvider`，由 `ModelRegistry` 自动发现。新的模型提供商可以实现 `Supports(string, ModelCreationContext)` 和 `Create(string, ModelCreationContext)` 来消费 context。

## Chat Model

**Chat Model** 是驱动 agent 对话与工具调用的 LLM，输入输出可以是文本之外的多模态内容。AgentScope .NET 当前提供以下 Chat Model 类：

| 提供商 | 模型类 | 说明 |
|--------|--------|------|
| OpenAI | `OpenAIChatModel` | Chat Completions API，兼容 vLLM 与 OpenAI 兼容端点（含 DeepSeek、Kimi 等） |
| Anthropic | `AnthropicChatModel` | Claude 模型，支持 prompt 缓存与 thinking |
| DashScope | `DashScopeChatModel` | Qwen 模型，多模态（视觉/音频/视频）、推理 |
| Gemini | `GeminiChatModel` | Google Gemini 模型，支持多模态 |
| Ollama | `OllamaChatModel` | 本地 LLM 托管，凭证可选 |

模型提供商凭证类随对应模型扩展模块提供，例如 `OpenAICredential`、`AnthropicCredential`、`DashScopeCredential`、`GeminiCredential`、`OllamaCredential`。OpenAI 兼容提供商的 `DeepSeekCredential`、`KimiCredential`、`XAICredential` 仍在 core 模块中可用。

### 创建 Chat Model

每个 Chat Model 通过 builder 构造，最常见的字段是 `ApiKey`、`ModelName`、`Stream`、`Formatter`、`DefaultOptions`。下面三个 tab 分别展示流式、工具调用与推理三种典型初始化场景：

::::{tab-set}
:::{tab-item} Streaming
```csharp
using AgentScope.Extensions.Model.DashScope;
using AgentScope.Extensions.Model.DashScope.Formatter;

DashScopeChatModel model =
        DashScopeChatModel.Builder()
                .WithApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
                .WithModelName("qwen-plus")
                .WithStream(true)
                .WithFormatter(new DashScopeChatFormatter())
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
                .WithApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
                .WithModelName("qwen-plus")
                .WithStream(false)
                .WithFormatter(new DashScopeChatFormatter())
                .WithDefaultOptions(
                        GenerateOptions.Builder()
                                .WithParallelToolCalls(false)
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
                .WithApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
                .WithModelName("qwen3-235b-a22b-thinking-2507")
                .WithStream(true)
                .EnableThinking(true)
                .WithFormatter(new DashScopeChatFormatter())
                .WithDefaultOptions(
                        GenerateOptions.Builder()
                                .WithThinkingBudget(2048)
                                .Build())
                .Build();
```
:::
::::

各 Chat Model 的 builder 共享的字段大致相同：

| 字段 | 类型 | 说明 |
|------|------|------|
| `ApiKey` | `string` | API key（部分提供商也支持 `Credential(...)` 方式注入） |
| `ModelName` | `string` | 模型标识符（如 `"qwen-plus"`） |
| `Stream` | `boolean` | 是否流式输出 |
| `DefaultOptions` | `GenerateOptions` | 提供商专属生成参数（`Temperature`、`MaxTokens`、`ThinkingBudget`、`ParallelToolCalls` 等） |
| `Formatter` | `IFormatter` | 覆盖默认的消息 formatter |
| `BaseUrl` | `string` | 自定义服务端点（如 OpenAI 兼容的反代） |

### 调用 Chat Model

`Model` 接口暴露统一的 `Stream(messages, tools, options)`，返回 `IAsyncEnumerable<ChatResponse>`：

```csharp
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Extensions.Model.DashScope;
using AgentScope.Extensions.Model.DashScope.Formatter;
using System.Collections.Generic;

DashScopeChatModel model =
        DashScopeChatModel.Builder()
                .WithApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
                .WithModelName("qwen-plus")
                .WithStream(true)
                .WithFormatter(new DashScopeChatFormatter())
                .Build();

await foreach (var chunk in model.Stream(
        new List<Msg> { new UserMessage("Count from 1 to 5.") },
        /* tools = */ new List<ToolSchema>(),
        GenerateOptions.Builder().Build()))
{
    Console.WriteLine("Chunk: " + chunk.Content);
}
Console.WriteLine("Stream completed");
```

`ChatResponse` 包含若干 content block（`TextBlock`、`ThinkingBlock`、`ToolUseBlock`、`DataBlock`）以及记录 token 数与耗时的 `ChatUsage`。

实际开发中通常不需要直接调模型，而是通过 `ReActAgent` 调度；要直连模型做轻量调用时，推荐参考 `agentscope-examples/documentation/.../model/ModelRegistryExample.cs`。

### 生成结构化输出

Agent 层提供把模型输出绑定到 C# POCO 的便捷重载，由 `ReActAgent.CallAsync(msgs, structuredOutputType, runtimeContext)` 暴露：

```csharp
using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
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
                        RuntimeContext.Empty());

WeatherInfo info = msg.GetStructuredData<WeatherInfo>();
```

实现细节：框架会基于目标 Type 合成强制结构化的工具调用，再校验并修复模型输出，最后把结果挂到 `Msg.Metadata` 的 `structured_output` 字段，供 `GetStructuredData<T>()` 直接反序列化。完整示例：`agentscope-examples/documentation/.../structuredoutput/StructuredOutputExample.cs`。

#### 结构化输出路径选择

框架提供两条结构化输出路径：

| 路径 | 条件 | 机制 |
|------|------|------|
| **Native** | `SupportsNativeStructuredOutput() == true` | 通过 `response_format` + `json_schema` 让模型直接输出合规 JSON |
| **Fallback**（默认） | `SupportsNativeStructuredOutput() == false` | 注入 `generate_response` 合成工具，模型通过 tool call 返回结构化数据 |

当 native 路径失败（如模型返回 400），框架会**自动降级**到 fallback 路径，无需用户干预。

#### 各模型提供商默认行为

| 模型提供商 | `SupportsNativeStructuredOutput` | 说明 |
|----------|----------------------------------|------|
| OpenAI (GPT-4o 等) | `true` | 原生支持 `json_schema` |
| OpenAI (DeepSeek/GLM formatter) | `false` | 不支持，自动走 fallback |
| DashScope | `false` | DashScope 原生端点仅支持 `json_object`，不支持 `json_schema`；框架默认走 fallback |
| Anthropic | `false`（默认） | — |

> **DashScope 用户注意**：DashScope 的思考模式（`EnableThinking(true)`）不支持结构化输出，框架会强制走 fallback 路径。

#### 显式配置

如果确认你的模型/端点支持 `json_schema`，可以通过 builder 开启 native 路径：

```csharp
DashScopeChatModel model = DashScopeChatModel.Builder()
        .WithApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
        .WithModelName("qwen-plus")
        .WithNativeStructuredOutput(true)  // 显式开启 native json_schema 路径
        .Build();
```

#### 结构化输出与工具调用共存

当 Agent 同时注册了工具并请求结构化输出时，部分 OpenAI 兼容 API（如 Kimi、Deepseek 等）会优先遵循 `response_format` 约束而跳过工具调用。设置 `NativeStructuredOutputWithTools(false)` 可解决此问题：

```csharp
OpenAIChatModel model = OpenAIChatModel.Builder()
        .WithApiKey("...")
        .WithBaseUrl("https://api.moonshot.cn/v1")
        .WithModelName("moonshot-v1-8k")
        .WithNativeStructuredOutputWithTools(false)
        .Build();
```

`DashScopeChatModel` 同样支持此配置。对于 OpenAI 原生模型（GPT-4o 等）无需设置。

### Formatter

**Formatter** 负责把 AgentScope 的 `Msg` 对象转换为各提供商 API 期望的请求载荷。它通过 Chat Model builder 的 `Formatter(...)` 字段配置。每个提供商内置两种 formatter：

| 类型 | 适用场景 |
|------|----------|
| **ChatFormatter**（默认） | 标准的单 agent 对话。每条 `Msg` 1:1 映射为一条 API 消息，保留原始角色（`User`、`Assistant`、`System`）。 |
| **MultiAgentFormatter** | 多 agent 场景，例如辩论、moderator。连续的 agent 消息会被聚合，并标注发送者名字。 |

切换到多 agent 模式只需传入 MultiAgent 变体，无需修改 agent 代码：

```csharp
using AgentScope.Extensions.Model.DashScope.Formatter;
using AgentScope.Extensions.Model.DashScope;

DashScopeChatModel model =
        DashScopeChatModel.Builder()
                .WithApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
                .WithModelName("qwen-plus")
                .WithStream(true)
                .WithFormatter(new DashScopeMultiAgentFormatter())
                .Build();
```

各模型提供商的 formatter 类现在随对应模型扩展模块一起提供：

| 模型提供商 | Chat | MultiAgent |
|---|---|---|
| DashScope | `DashScopeChatFormatter` | `DashScopeMultiAgentFormatter` |
| OpenAI | `OpenAIChatFormatter` | `OpenAIMultiAgentFormatter` |
| Anthropic | `AnthropicChatFormatter` | `AnthropicMultiAgentFormatter` |
| Gemini | `GeminiChatFormatter` | `GeminiMultiAgentFormatter` |
| Ollama | `OllamaChatFormatter` | `OllamaMultiAgentFormatter` |

如果提供商的载荷格式不属于以上几种，开发者可以实现 `IFormatter<TReq, TResp, TParams>` 接口（位于 `AgentScope.Core.Formatter`），并通过同一个 `Formatter(...)` 字段传入。

### 自定义模型提供商

接入自定义模型提供商的最小路径是：实现一个 `CredentialBase` 子类与一个 `ChatModelBase` 子类。

#### 步骤 1：定义 Credential

继承 `CredentialBase`，实现 `GetChatModelType()`：

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

    public string ApiKey => _apiKey;
    public string BaseUrl => _baseUrl;

    public override Type GetChatModelType() => typeof(MyProviderChatModel);
}
```

#### 步骤 2：实现 Chat Model

继承 `ChatModelBase`，实现 `DoStream`：

```csharp
using AgentScope.Core.Message;
using AgentScope.Core.Model;
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

    protected override IAsyncEnumerable<ChatResponse> DoStream(
            List<Msg> messages, List<ToolSchema> tools, GenerateOptions options)
    {
        // 调用提供商 API、把响应封装为 ChatResponse 流
        return AsyncEnumerable.Empty<ChatResponse>();
    }
}
```

#### 步骤 3：注册到 ModelRegistry（可选）

`ModelRegistry` 可以让 `ReActAgent.Builder().WithModel("provider:model-name")` 字符串化解析模型：

```csharp
using AgentScope.Core.Model;

ModelRegistry.RegisterFactory(
        "myprov:.*",
        modelId => new MyProviderChatModel(
                new MyProviderCredential(Environment.GetEnvironmentVariable("MYPROV_API_KEY"), null),
                modelId["myprov:".Length..]));

// 之后即可：
// ReActAgent.Builder().WithModel("myprov:my-model-v1")...
```

## 前端集成

### 什么是 ModelCard

`ModelCard`（`AgentScope.Core.Credential.ModelCard`）是对模型能力与约束的声明式描述，用于驱动前端 —— 模型选择器、参数表单、能力开关都可以基于它动态渲染，无需在前端硬编码任何提供商相关的逻辑。

当前 `ModelCard` 是一个最小化的 record，包含：

| 属性 | 类型 | 说明 |
|------|------|------|
| `ModelName` | `string` | 模型标识符（例如 `"claude-sonnet-4-6"`） |
| `DisplayName` | `string` | 用于展示的可读名称（例如 `"Claude Sonnet 4.6"`） |
| `ContextSize` | `int` | 最大上下文窗口（token 数） |

:::{note}
ModelCard 属性当前最小化；能力标记（输入/输出 MIME 类型）与参数 schema 将随模型发现基础设施完善而扩展。
:::

### 获取 ModelCard

通过 `CredentialBase.ListModels()` 获取 Model Card，返回 `Task<List<ModelCard>>`：

```csharp
using AgentScope.Core.Credential;
using AgentScope.Extensions.Model.Anthropic.Credential;
using System.Collections.Generic;

AnthropicCredential cred = new AnthropicCredential(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
List<ModelCard> cards = await cred.ListModels();

foreach (var card in cards)
{
    Console.WriteLine(
            card.ModelName + ": context=" + card.ContextSize);
}
```

`GetChatModelType()` 返回对应的 `ChatModelBase` 子类，可用于反向构造默认 model：

```csharp
Type modelType = cred.GetChatModelType();
```

这种设计让前端只需一个 credential，就能发现该模型提供商下的可用模型 —— 无需任何硬编码的提供商逻辑。
