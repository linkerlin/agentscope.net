# DashScope 模型

`AgentScope.Extensions.Model.DashScope` 接入阿里云 DashScope Qwen Model，包括多模态和推理能力的 Qwen 模型。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Model.DashScope" Version="*" />
```

## ModelRegistry

设置 `DASHSCOPE_API_KEY` 后，可以使用 `dashscope:<model>`，也可以使用 Qwen 短名：

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("dashscope:qwen-plus") // 底层由 ModelRegistry.Resolve(modelId) 解析
    .Build();
```

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("qwen-plus") // 底层由 ModelRegistry.Resolve(modelId) 解析
    .Build();
```

## 显式 builder

需要 DashScope 专属配置时使用 builder，例如 endpoint type、thinking、search、encryption：

```csharp
using AgentScope.Extensions.Model.DashScope;

DashScopeChatModel model = DashScopeChatModel.Builder()
    .ApiKey(System.Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
    .ModelName("qwen-plus")
    .Stream(true)
    .Build();
```

## Spring Boot

Spring Boot 应用可以使用 DashScope starter：

```xml
<PackageReference Include="AgentScope.DashScope" Version="*" />
```

完整 builder 选项、formatter、credential 和 registry context 细节见 [模型](../../docs/building-blocks/model.md)。
