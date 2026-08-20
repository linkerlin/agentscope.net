# Gemini 模型

`AgentScope.Core.Model.Gemini.GeminiModel` 接入 Google Gemini 模型。

## 构造函数

```csharp
GeminiModel(string modelName = "gemini-pro", string? apiKey = null,
    string? baseUrl = null, GenerateOptions? defaultOptions = null)
```

## 最小示例

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.Gemini;

string key = System.Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var model = new GeminiModel("gemini-2.0-flash", key);

string response = await model.GenerateAsync(
    new ModelRequest
    {
        Messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("Hello").Build()
        }
    }).Result.Text;
```

## Agent 集成

```csharp
using AgentScope.Core.Model.Gemini;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new GeminiModel("gemini-2.0-flash", key))
    .Build();
```

## 流式调用

`GeminiModel` **不实现** `IStreamingChatModel`，不支持流式生成。请使用 `GenerateAsync` 获取完整响应。

## 注意事项

- `modelName` 缺省为 `"gemini-pro"`。
- `apiKey` 缺省时建议从 `GEMINI_API_KEY` 环境变量读取。
- `GeminiModel` 无需 formatter 参数，构造函数比 OpenAI 更简洁。
- 自定义 `GenerateOptions` 可通过构造函数传入。

完整接口说明见 [模型](../../docs/building-blocks/model.md)。
