# DashScope Model

`AgentScope.Extensions.Model.DashScope` integrates Alibaba Cloud DashScope Qwen models, including multimodal and reasoning-capable Qwen models.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Model.DashScope" Version="$(AgentScopeVersion)" />
```

## ModelRegistry

Set `DASHSCOPE_API_KEY`, then use either `dashscope:<model>` or the Qwen short form:

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("dashscope:qwen-plus") // Resolved internally by ModelRegistry.Resolve(modelId)
    .Build();
```

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("qwen-plus") // Resolved internally by ModelRegistry.Resolve(modelId)
    .Build();
```

## Explicit builder

Use the builder when you need DashScope-specific options such as endpoint type, thinking, search, encryption:

```csharp
using AgentScope.Extensions.Model.DashScope;

DashScopeChatModel model = DashScopeChatModel.Builder()
    .ApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
    .ModelName("qwen-plus")
    .Stream(true)
    .Build();
```

## Spring Boot

Spring Boot applications can use the DashScope starter:

```xml
<PackageReference Include="AgentScope.DashScope.SpringBoot.Starter" Version="$(AgentScopeVersion)" />
```

Full builder options, formatters, credentials, and registry context details are covered in [Model](../../docs/building-blocks/model.md).
