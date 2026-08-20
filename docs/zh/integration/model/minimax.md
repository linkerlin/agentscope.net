# MiniMax 模型

MiniMax 使用 OpenAI 兼容端点，通过 `OpenAIModel` 接入，无专用模型类。

## 构造函数示例

```csharp
new OpenAIModel("MiniMax-M3", apiKey, "https://api.minimax.chat/v1")
```

## 最小示例

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;

string key = System.Environment.GetEnvironmentVariable("MINIMAX_API_KEY");
var model = new OpenAIModel("MiniMax-M3", key,
    "https://api.minimax.chat/v1");

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
    .Model(new OpenAIModel("MiniMax-M3", key,
        "https://api.minimax.chat/v1"))
    .Build();
```

## 流式调用

`OpenAIModel` 实现 `IStreamingChatModel`，MiniMax 端点同样支持：

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## 与专用模型的差异

- AgentScope 中没有 `MiniMaxModel` 专用类，所有能力来自 `OpenAIModel`。
- 环境变量建议：`MINIMAX_API_KEY`。
- MiniMax-M3 支持适应性思考（adaptive thinking），可通过 `GenerateOptions` 的 `defaultOptions` 参数配置。
- 自定义 formatter 可通过 `OpenAIModel` 的 `formatter` 参数传入。

完整接口说明见 [模型](../../docs/building-blocks/model.md)。
