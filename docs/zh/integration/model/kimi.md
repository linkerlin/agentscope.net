# Kimi 模型

Kimi（月之暗面 / Moonshot AI）使用 OpenAI 兼容端点，通过 `OpenAIModel` 接入，无专用模型类。

## 构造函数示例

```csharp
new OpenAIModel("moonshot-v1-8k", apiKey, "https://api.moonshot.cn/v1")
```

## 最小示例

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;

string key = System.Environment.GetEnvironmentVariable("MOONSHOT_API_KEY");
var model = new OpenAIModel("moonshot-v1-8k", key,
    "https://api.moonshot.cn/v1");

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
    .Model(new OpenAIModel("moonshot-v1-8k", key,
        "https://api.moonshot.cn/v1"))
    .Build();
```

## 流式调用

`OpenAIModel` 实现 `IStreamingChatModel`，Kimi 端点同样支持：

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## 与专用模型的差异

- AgentScope 中没有 `KimiModel` 专用类，所有能力来自 `OpenAIModel`。
- 环境变量建议：`MOONSHOT_API_KEY` 或 `KIMI_API_KEY`。
- Kimi 部分模型（如 kimi-k3）支持思考模式，可通过 `GenerateOptions` 传入 `reasoning_effort` 等参数。
- 自定义 formatter 可通过 `OpenAIModel` 的 `formatter` 参数传入。

完整接口说明见 [模型](../../docs/building-blocks/model.md)。
