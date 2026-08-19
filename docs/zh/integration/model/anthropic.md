# Anthropic 模型

`AgentScope.Extensions.Model.Anthropic` 接入 Anthropic Claude Model，并提供 Anthropic 专属 formatter 和请求 DTO 支持。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Model.Anthropic" Version="*" />
```

## ModelRegistry

设置 `ANTHROPIC_API_KEY` 后，使用 `anthropic:<model>` 字符串 id：

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("anthropic:claude-sonnet-4.5") // 底层由 ModelRegistry.Resolve(modelId) 解析
    .Build();
```

## 显式 builder

需要自定义 endpoint、formatter、transport、prompt caching、thinking 或生成参数时，使用 builder：

```csharp
using AgentScope.Extensions.Model.Anthropic;

AnthropicChatModel model = AnthropicChatModel.Builder()
    .ApiKey(System.Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))
    .ModelName("claude-sonnet-4.5")
    .Stream(true)
    .Build();
```

## Spring Boot

Spring Boot 应用可以使用 Anthropic starter：

```xml
<PackageReference Include="AgentScope.Anthropic" Version="*" />
```

完整 builder 选项、formatter、credential 和 registry context 细节见 [模型](../../docs/building-blocks/model.md)。
