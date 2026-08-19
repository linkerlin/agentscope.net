# Ollama Model

`AgentScope.Extensions.Model.Ollama` integrates locally hosted Ollama models. It is useful for local development, private deployments, and offline model serving.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Model.Ollama" Version="$(AgentScopeVersion)" />
```

## ModelRegistry

Use the `ollama:<model>` id. `OLLAMA_BASE_URL` is optional and defaults to the local Ollama endpoint when omitted.

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("ollama:llama3") // Resolved internally by ModelRegistry.Resolve(modelId)
    .Build();
```

## Explicit builder

Use the builder when you need a non-default Ollama endpoint, formatter, transport, proxy, or Ollama options:

```csharp
using AgentScope.Extensions.Model.Ollama;

OllamaChatModel model = OllamaChatModel.Builder()
    .ModelName("llama3")
    .BaseUrl("http://localhost:11434")
    .Build();
```

## Spring Boot

Spring Boot applications can use the Ollama starter:

```xml
<PackageReference Include="AgentScope.Ollama.SpringBoot.Starter" Version="$(AgentScopeVersion)" />
```

Configure the local Ollama model with `agentscope.model.provider=ollama`. The base URL
is optional and defaults to `http://localhost:11434`:

```yaml
agentscope:
  model:
    provider: ollama
  ollama:
    model-name: llama3
    # base-url: http://localhost:11434
```

Full builder options, formatters, credentials, and registry context details are covered in [Model](../../docs/building-blocks/model.md).
