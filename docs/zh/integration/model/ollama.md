# Ollama 模型

`AgentScope.Extensions.Model.Ollama` 接入本地托管的 Ollama 模型，适合本地开发、私有化部署和离线模型服务。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Model.Ollama" Version="*" />
```

## ModelRegistry

使用 `ollama:<model>` 字符串 id。`OLLAMA_BASE_URL` 是可选环境变量，不设置时默认使用本地 Ollama endpoint。

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("ollama:llama3") // 底层由 ModelRegistry.Resolve(modelId) 解析
    .Build();
```

## 显式 builder

需要非默认 Ollama endpoint、formatter、transport、proxy 或 Ollama options 时，使用 builder：

```csharp
using AgentScope.Extensions.Model.Ollama;

OllamaChatModel model = OllamaChatModel.Builder()
    .ModelName("llama3")
    .BaseUrl("http://localhost:11434")
    .Build();
```

## Spring Boot

Spring Boot 应用可以使用 Ollama starter：

```xml
<PackageReference Include="AgentScope.Ollama" Version="*" />
```

通过 `agentscope.model.provider=ollama` 配置本地 Ollama 模型。base URL 为可选项，默认是
`http://localhost:11434`：

```yaml
agentscope:
  model:
    provider: ollama
  ollama:
    model-name: llama3
    # base-url: http://localhost:11434
```

完整 builder 选项、formatter、credential 和 registry context 细节见 [模型](../../docs/building-blocks/model.md)。
