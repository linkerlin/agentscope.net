# 模型提供商

AgentScope .NET 通过 `AgentScope.Core.Model` 命名空间下的专用模型类接入各模型 API。所有模型类直接构造，无需字符串 id 或注册表。

| 模型类 | 命名空间 | 构造要点 | 流式支持 |
|--------|----------|----------|----------|
| `OpenAIModel` | `AgentScope.Core.Model.OpenAI` | `new OpenAIModel(modelName, apiKey, baseUrl?)` | 是（`IStreamingChatModel`） |
| `DashScopeModel` | `AgentScope.Core.Model.DashScope` | `new DashScopeModel(modelName, apiKey, baseUrl?)` | 是 |
| `AnthropicModel` | `AgentScope.Core.Model.Anthropic` | `new AnthropicModel(modelName, apiKey, baseUrl?)` | 是 |
| `GeminiModel` | `AgentScope.Core.Model.Gemini` | `new GeminiModel(modelName?, apiKey?, baseUrl?)` | 否 |
| `DeepSeekModel` | `AgentScope.Core.Model.DeepSeek` | `new DeepSeekModel(modelName?, apiKey?)`（继承 `OpenAIModel`） | 是 |
| `OllamaModel` | `AgentScope.Core.Model.Ollama` | `new OllamaModel(modelName?, baseUrl?)`（继承 `OpenAIModel`） | 是 |
| `MockModel` | `AgentScope.Core.Model` | `new MockModel()` 或 `MockModel.Builder()...Build()` | 否 |
| `ModelFactory` | `AgentScope.Core.Model` | `ModelFactory.Create(provider, modelName, apiKey, baseUrl?)` | 取决于 provider |

GLM（智谱）、Kimi（月之暗面）、MiniMax 通过 OpenAI 兼容端点接入 `OpenAIModel`。

- [OpenAI](openai.md)
- [DeepSeek](deepseek.md)
- [GLM](glm.md)
- [Kimi](kimi.md)
- [MiniMax](minimax.md)
- [DashScope](dashscope.md)
- [Gemini](gemini.md)
- [Anthropic](anthropic.md)
- [Ollama](ollama.md)

更多信息见 [模型](../../docs/building-blocks/model.md)。
