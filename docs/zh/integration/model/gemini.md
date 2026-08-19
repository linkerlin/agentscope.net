# Gemini 模型

`AgentScope.Extensions.Model.Gemini` 接入 Google Gemini 模型。它支持 Gemini API，也可以通过显式配置走 Vertex AI 路径。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Model.Gemini" Version="*" />
```

## ModelRegistry

设置 `GEMINI_API_KEY` 后，使用 `gemini:<model>` 字符串 id：

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("gemini:gemini-2.0-flash") // 底层由 ModelRegistry.Resolve(modelId) 解析
    .Build();
```

## 显式 builder

需要自定义 API 设置、Vertex AI credentials、formatter、transport 或生成参数时，使用 builder：

```csharp
using AgentScope.Extensions.Model.Gemini;

GeminiChatModel model = GeminiChatModel.Builder()
    .ApiKey(System.Environment.GetEnvironmentVariable("GEMINI_API_KEY"))
    .ModelName("gemini-2.0-flash")
    .StreamEnabled(true)
    .Build();
```

## Spring Boot

Spring Boot 应用可以使用 Gemini starter：

```xml
<PackageReference Include="AgentScope.Gemini" Version="*" />
```

完整 builder 选项、formatter、credential 和 registry context 细节见 [模型](../../docs/building-blocks/model.md)。
