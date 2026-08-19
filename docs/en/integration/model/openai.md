# OpenAI Model

`AgentScope.Extensions.Model.OpenAI` integrates OpenAI Chat Completions-style models. It is also the module to use for OpenAI-compatible endpoints such as DeepSeek, GLM, Kimi, MiniMax, and similar services when their wire format follows the OpenAI API.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Model.OpenAI" Version="$(AgentScopeVersion)" />
```

## ModelRegistry

Set `OPENAI_API_KEY`, then use the `openai:<model>` id:

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("openai:gpt-4.1-mini") // Resolved internally by ModelRegistry.Resolve(modelId)
    .Build();
```

## Explicit builder

Use the builder when you need a custom endpoint, formatter, transport, or generation options:

```csharp
using AgentScope.Extensions.Model.OpenAI;

OpenAIChatModel model = OpenAIChatModel.Builder()
    .ApiKey(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .ModelName("gpt-4.1-mini")
    .Stream(true)
    .Build();
```

## Spring Boot

Spring Boot applications can use the OpenAI starter:

```xml
<PackageReference Include="AgentScope.OpenAI.SpringBoot.Starter" Version="$(AgentScopeVersion)" />
```

Full builder options, formatters, credentials, and registry context details are covered in [Model](../../docs/building-blocks/model.md).
