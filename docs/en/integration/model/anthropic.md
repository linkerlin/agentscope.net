# Anthropic Model

`AgentScope.Extensions.Model.Anthropic` integrates Anthropic Claude models, including Anthropic-specific formatter and request DTO support.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Model.Anthropic" Version="$(AgentScopeVersion)" />
```

## ModelRegistry

Set `ANTHROPIC_API_KEY`, then use the `anthropic:<model>` id:

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("anthropic:claude-sonnet-4.5") // Resolved internally by ModelRegistry.Resolve(modelId)
    .Build();
```

## Explicit builder

Use the builder when you need a custom endpoint, formatter, transport, prompt caching, thinking, or generation options:

```csharp
using AgentScope.Extensions.Model.Anthropic;

AnthropicChatModel model = AnthropicChatModel.Builder()
    .ApiKey(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))
    .ModelName("claude-sonnet-4.5")
    .Stream(true)
    .Build();
```

## Spring Boot

Spring Boot applications can use the Anthropic starter:

```xml
<PackageReference Include="AgentScope.Anthropic.SpringBoot.Starter" Version="$(AgentScopeVersion)" />
```

Full builder options, formatters, credentials, and registry context details are covered in [Model](../../docs/building-blocks/model.md).
