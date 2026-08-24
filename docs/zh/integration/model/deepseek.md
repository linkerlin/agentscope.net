# DeepSeek 模型

`AgentScope.Core.Model.DeepSeek.DeepSeekModel` 接入 DeepSeek API，继承 `OpenAIModel`。

## 构造函数

```csharp
DeepSeekModel(string modelName = "deepseek-chat", string? apiKey = null)
```

`apiKey` 缺省时自动读取 `DEEPSEEK_API_KEY` 环境变量。

## 最小示例

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.DeepSeek;

var model = new DeepSeekModel("deepseek-chat");

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
using AgentScope.Core.Model.DeepSeek;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new DeepSeekModel("deepseek-chat"))
    .Build();
```

## 流式调用

`DeepSeekModel` 继承 `OpenAIModel`，实现 `IStreamingChatModel`：

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## 注意事项

- `DeepSeekModel` 继承 `OpenAIModel`，因此支持 `OpenAIModel` 构造函数中更多的参数（自定义 `baseUrl`、`client`、`formatter`、`defaultOptions`），但 `DeepSeekModel` 构造函数仅暴露 `modelName` 和 `apiKey`。
- 如需自定义端点，直接使用 `new OpenAIModel("deepseek-chat", key, "https://api.deepseek.com")`。

完整接口说明见 [模型](../../docs/building-blocks/model.md)。
