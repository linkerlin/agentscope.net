# GLM 模型

GLM（智谱 / Z.AI）使用 OpenAI 兼容端点，通过 `OpenAIModel` 接入，无专用模型类。

## 构造函数示例

```csharp
new OpenAIModel("glm-4-plus", apiKey, "https://open.bigmodel.cn/api/paas/v4")
```

## 最小示例

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.OpenAI;

string key = System.Environment.GetEnvironmentVariable("ZHIPUAI_API_KEY");
var model = new OpenAIModel("glm-4-plus", key,
    "https://open.bigmodel.cn/api/paas/v4");

string response = await model.GenerateAsync(
    new ModelRequest
    {
        Messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("What is GLM?").Build()
        }
    }).Result.Text;
```

## Agent 集成

```csharp
using AgentScope.Core.Model.OpenAI;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new OpenAIModel("glm-4-plus", key,
        "https://open.bigmodel.cn/api/paas/v4"))
    .Build();
```

## 流式调用

`OpenAIModel` 实现 `IStreamingChatModel`，GLM 端点同样支持：

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## 与专用模型的差异

- AgentScope 中没有 `GLMModel` 专用类，所有能力来自 `OpenAIModel`。
- 环境变量建议：`ZHIPUAI_API_KEY` 或 `GLM_API_KEY`。
- 自定义 formatter 可通过 `OpenAIModel` 的 `formatter` 参数传入。
- 如需思考模式等 GLM 特有参数，通过 `GenerateOptions` 的 `defaultOptions` 参数传入。

完整接口说明见 [模型](../../docs/building-blocks/model.md)。
