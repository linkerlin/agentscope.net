# OpenAI 模型

`AgentScope.Core.Model.OpenAI.OpenAIModel` 接入 OpenAI Chat Completions API。OpenAI 兼容端点（如 DeepSeek、GLM、Kimi、MiniMax）也使用此类。

## 构造函数

```csharp
OpenAIModel(string modelName, string? apiKey = null, string? baseUrl = null,
    OpenAIClient? client = null, OpenAIChatFormatter? formatter = null,
    GenerateOptions? defaultOptions = null)
```

## 最小示例

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;

string key = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var model = new OpenAIModel("gpt-4o", key);

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
using AgentScope.Core.Model.OpenAI;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new OpenAIModel("gpt-4o", key))
    .Build();
```

## 流式调用

`OpenAIModel` 实现了 `IStreamingChatModel`：

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## 注意事项

- `apiKey` 缺省时从环境变量 `OPENAI_API_KEY` 读取。
- `baseUrl` 缺省使用 OpenAI 官方端点。
- 自定义 `OpenAIClient`、`OpenAIChatFormatter` 或 `GenerateOptions` 可通过构造函数传入。

完整接口说明见 [模型](../../docs/building-blocks/model.md)。
