# OpenAI 模型

`AgentScope.Extensions.Model.OpenAI` 接入 OpenAI Chat Completions 风格的模型。OpenAI 兼容端点也使用这个适配模块，例如 DeepSeek、GLM、Kimi、MiniMax 等遵循 OpenAI API 载荷格式的服务。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Model.OpenAI" Version="*" />
```

## ModelRegistry

设置 `OPENAI_API_KEY` 后，使用 `openai:<model>` 字符串 id：

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("openai:gpt-4.1-mini") // 底层由 ModelRegistry.Resolve(modelId) 解析
    .Build();
```

## 显式 builder

需要自定义 endpoint、formatter、transport 或生成参数时，使用 builder：

```csharp
using AgentScope.Extensions.Model.OpenAI;

OpenAIChatModel model = OpenAIChatModel.Builder()
    .ApiKey(System.Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
    .ModelName("gpt-4.1-mini")
    .Stream(true)
    .Build();
```

## Spring Boot

Spring Boot 应用可以使用 OpenAI starter：

```xml
<PackageReference Include="AgentScope.OpenAI" Version="*" />
```

完整 builder 选项、formatter、credential 和 registry context 细节见 [模型](../../docs/building-blocks/model.md)。
