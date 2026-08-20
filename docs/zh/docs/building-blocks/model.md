---
title: "模型"
description: "IModel 模型体系：内置提供商、流式接口、ModelFactory"
---

## 概述

所有模型都在 **`AgentScope.Core`** 包内（没有独立的模型扩展包），实现统一接口：

```csharp
public interface IModel
{
    string ModelName { get; }
    IObservable<ModelResponse> Generate(ModelRequest request);    // 响应式（System.Reactive）
    Task<ModelResponse> GenerateAsync(ModelRequest request);      // Task 异步（主要方式）
}

public interface IStreamingChatModel
{
    IAsyncEnumerable<ChatResponse> GenerateStreamAsync(
        List<Msg> messages, CancellationToken cancellationToken = default);
}
```

- `ModelRequest`：`Messages`（`List<Msg>`）+ 可选 `Options`（`Dictionary<string, object>`，如 temperature）。
- `ModelResponse`：`Text` / `Metadata` / `Success` / `Error`。
- `ChatResponse` 继承 `ModelResponse`，额外含 `Id` / `Content` / `ToolCalls`（`List<ToolCallInfo>`）/ `Usage`（`ChatUsage`：InputTokens、OutputTokens、TotalTokens、TimeSeconds）/ `Model` / `StopReason` / `IsComplete`。

`EnhancedReActAgent` 的流式循环会自动检测模型是否实现 `IStreamingChatModel`，实现了则逐块产出 `ReasoningChunk` 事件，否则整段返回。

## 内置模型

| 类 | 命名空间 | 构造签名 | 流式 |
|----|----------|----------|------|
| `OpenAIModel` | `AgentScope.Core.Model.OpenAI` | `OpenAIModel(string modelName, string? apiKey = null, string? baseUrl = null, OpenAIClient? client = null, OpenAIChatFormatter? formatter = null, GenerateOptions? defaultOptions = null)` | ✅（两个重载，可传 `GenerateOptions`） |
| `DashScopeModel` | `AgentScope.Core.Model.DashScope` | `DashScopeModel(string modelName, string? apiKey = null, string? baseUrl = null, DashScopeChatFormatter? formatter = null, GenerateOptions? defaultOptions = null)` | ✅ |
| `AnthropicModel` | `AgentScope.Core.Model.Anthropic` | `AnthropicModel(string modelName, string? apiKey = null, string? baseUrl = null, AnthropicChatFormatter? formatter = null, GenerateOptions? defaultOptions = null)` | ✅ |
| `GeminiModel` | `AgentScope.Core.Model.Gemini` | `GeminiModel(string modelName = "gemini-pro", string? apiKey = null, string? baseUrl = null, GenerateOptions? defaultOptions = null)` | ❌（未实现 IStreamingChatModel） |
| `DeepSeekModel` | `AgentScope.Core.Model.DeepSeek` | `DeepSeekModel(string modelName = "deepseek-chat", string? apiKey = null)`——apiKey 缺省读 `DEEPSEEK_API_KEY` 环境变量 | ✅（继承 OpenAIModel） |
| `OllamaModel` | `AgentScope.Core.Model.Ollama` | `OllamaModel(string modelName = "llama2", string? baseUrl = null)`——baseUrl 缺省探测本机 Ollama | ✅（继承 OpenAIModel） |
| `MockModel` | `AgentScope.Core.Model` | `MockModel(string modelName = "mock-model")` 或 `MockModel.Builder().ModelName("mock").Build()` | ✅（回显输入） |

### 使用示例

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.DashScope;

// DashScope（通义千问）
IModel dashscope = new DashScopeModel("qwen-plus", Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"));

// OpenAI 兼容服务（自定义 baseUrl）
IModel proxy = new OpenAIModel("gpt-4o", apiKey, "https://your-proxy.example.com/v1");

// DeepSeek / Ollama：极简构造
IModel deepseek = new DeepSeekModel();                     // deepseek-chat + 环境变量
IModel local    = new OllamaModel("qwen3:8b");             // 本机 Ollama

// 测试替身
IModel mock = MockModel.Builder().ModelName("mock-model").Build();
```

### 直接调用模型

```csharp
var request = new ModelRequest
{
    Messages = [Msg.Builder().Role("user").TextContent("你好").Build()],
    Options = new Dictionary<string, object> { ["temperature"] = 0.7 }
};

ModelResponse resp = await model.GenerateAsync(request);
Console.WriteLine($"{resp.Success}: {resp.Text}");

// 流式
await foreach (ChatResponse chunk in ((IStreamingChatModel)model).GenerateStreamAsync(request.Messages))
{
    Console.Write(chunk.Content);
}
```

## ModelFactory

`AgentScope.Core.ModelFactory` 是静态工具类，按 provider 字符串创建模型：

```csharp
// 签名：Create(string provider, string modelName, string apiKey, string? baseUrl = null)
IModel model = ModelFactory.Create("dashscope", "qwen-plus", apiKey);

// 或从字典配置
IModel model2 = ModelFactory.Create(new Dictionary<string, string>
{
    ["provider"] = "openai",
    ["model"] = "gpt-4o",
    ["apiKey"] = apiKey,
    ["baseUrl"] = "https://api.openai.com/v1"      // 可选
});
```

支持的 provider：`openai` / `azure` / `anthropic` / `deepseek` / `gemini` / `dashscope` / `ollama`。扩展方法 `ModelFactoryExtensions.IsSupportedProvider(provider)` / `GetDefaultModel(provider)` / `GetSupportedProviders()` 可查询。

## Formatter 体系

各提供商的消息格式化器位于 `AgentScope.Core.Formatter.*`：

| Formatter | 命名空间 | 构造 |
|-----------|----------|------|
| `OpenAIChatFormatter` | `AgentScope.Core.Formatter.OpenAI` | `(string modelName)` |
| `DashScopeChatFormatter` | `AgentScope.Core.Formatter.DashScope` | `()`（默认 qwen-plus）或 `(string modelName)` |
| `AnthropicChatFormatter` | `AgentScope.Core.Formatter.Anthropic` | `()`（默认 claude-3-5-sonnet）或 `(string modelName)` |
| `GeminiFormatter` | `AgentScope.Core.Formatter.Gemini` | `(GenerateOptions? defaultOptions = null)` |

模型在构造时自动创建默认 Formatter；需要自定义格式化行为时通过构造参数注入（GeminiModel 除外，通过 `GenerateOptions` 配置）。

:::{note}
`OpenAIModel`、`DashScopeModel`、`AnthropicModel`、`GeminiModel` 均继承抽象基类 `ModelBase`（提供 `ModelName` 只读存储）；`DeepSeekModel` 与 `OllamaModel` 直接继承 `OpenAIModel`。
:::

## 与 Agent 集成

```csharp
EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(new DashScopeModel("qwen-plus", apiKey))    // 必填
    .Build();

// HarnessAgent 通过 HarnessAgentBuilder.WithModel(IModel) 传入
HarnessAgent harness = new HarnessAgentBuilder()
    .WithModel(new OpenAIModel("gpt-4o", apiKey))
    .Build();
```

## 相关文档

- [智能体](./agent.md)
- [模型集成速查](../../integration/model/index.md)
