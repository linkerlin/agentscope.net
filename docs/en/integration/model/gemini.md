# Gemini Model

`AgentScope.Extensions.Model.Gemini` integrates Google Gemini models through the Gemini API and supports the Vertex AI path through explicit configuration.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Model.Gemini" Version="$(AgentScopeVersion)" />
```

## ModelRegistry

Set `GEMINI_API_KEY`, then use the `gemini:<model>` id:

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("gemini:gemini-2.0-flash") // Resolved internally by ModelRegistry.Resolve(modelId)
    .Build();
```

## Explicit builder

Use the builder when you need custom API settings, Vertex AI credentials, formatter, transport, or generation options:

```csharp
using AgentScope.Extensions.Model.Gemini;

GeminiChatModel model = GeminiChatModel.Builder()
    .ApiKey(Environment.GetEnvironmentVariable("GEMINI_API_KEY"))
    .ModelName("gemini-2.0-flash")
    .StreamEnabled(true)
    .Build();
```

## Spring Boot

Spring Boot applications can use the Gemini starter:

```xml
<PackageReference Include="AgentScope.Gemini.SpringBoot.Starter" Version="$(AgentScopeVersion)" />
```

Full builder options, formatters, credentials, and registry context details are covered in [Model](../../docs/building-blocks/model.md).
