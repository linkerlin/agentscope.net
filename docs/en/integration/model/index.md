# Model Providers

AgentScope .NET connects to model APIs through dedicated model classes in the `AgentScope.Core.Model` namespace. All model classes are constructed directly — no string IDs or registries required.

| Model Class | Namespace | Construction | Streaming |
|-------------|-----------|-------------|-----------|
| `OpenAIModel` | `AgentScope.Core.Model.OpenAI` | `new OpenAIModel(modelName, apiKey, baseUrl?)` | Yes (`IStreamingChatModel`) |
| `DashScopeModel` | `AgentScope.Core.Model.DashScope` | `new DashScopeModel(modelName, apiKey, baseUrl?)` | Yes |
| `AnthropicModel` | `AgentScope.Core.Model.Anthropic` | `new AnthropicModel(modelName, apiKey, baseUrl?)` | Yes |
| `GeminiModel` | `AgentScope.Core.Model.Gemini` | `new GeminiModel(modelName?, apiKey?, baseUrl?)` | No |
| `DeepSeekModel` | `AgentScope.Core.Model.DeepSeek` | `new DeepSeekModel(modelName?, apiKey?)` (extends `OpenAIModel`) | Yes |
| `OllamaModel` | `AgentScope.Core.Model.Ollama` | `new OllamaModel(modelName?, baseUrl?)` (extends `OpenAIModel`) | Yes |
| `MockModel` | `AgentScope.Core.Model` | `new MockModel()` or `MockModel.Builder()...Build()` | No |
| `ModelFactory` | `AgentScope.Core.Model` | `ModelFactory.Create(provider, modelName, apiKey, baseUrl?)` | Depends on provider |

GLM (Zhipu AI), Kimi (Moonshot AI), and MiniMax connect through OpenAI-compatible endpoints using `OpenAIModel`.

- [OpenAI](openai.md)
- [DeepSeek](deepseek.md)
- [GLM](glm.md)
- [Kimi](kimi.md)
- [MiniMax](minimax.md)
- [DashScope](dashscope.md)
- [Gemini](gemini.md)
- [Anthropic](anthropic.md)
- [Ollama](ollama.md)

For details see [Model](../../docs/building-blocks/model.md).
