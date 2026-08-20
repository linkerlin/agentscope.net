# Anthropic 模型

`AgentScope.Core.Model.Anthropic.AnthropicModel` 接入 Anthropic Claude 系列模型。

## 构造函数

```csharp
AnthropicModel(string modelName, string? apiKey = null, string? baseUrl = null,
    AnthropicChatFormatter? formatter = null, GenerateOptions? defaultOptions = null)
```

常量：`DefaultBaseUrl = "https://api.anthropic.com"`、`MessagesEndpoint = "/v1/messages"`。

## 最小示例

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.Anthropic;

string key = System.Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
var model = new AnthropicModel("claude-sonnet-4-20250514", key);

string response = await model.GenerateAsync(
    new ModelRequest
    {
        Messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("What is AgentScope?").Build()
        }
    }).Result.Text;
```

## Agent 集成

```csharp
using AgentScope.Core.Model.Anthropic;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new AnthropicModel("claude-sonnet-4-20250514", key))
    .Build();
```

## 流式调用

`AnthropicModel` 实现了 `IStreamingChatModel`：

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## 注意事项

- `apiKey` 缺省时建议从 `ANTHROPIC_API_KEY` 环境变量读取。
- 端点使用 Messages API（`/v1/messages`），与 OpenAI Chat Completions 格式不同。
- 自定义 `AnthropicChatFormatter` 或 `GenerateOptions` 可通过构造函数传入。

完整接口说明见 [模型](../../docs/building-blocks/model.md)。
