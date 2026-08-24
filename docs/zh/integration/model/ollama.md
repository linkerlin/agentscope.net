# Ollama 模型

`AgentScope.Core.Model.Ollama.OllamaModel` 接入本地 Ollama 服务，继承 `OpenAIModel`。

## 构造函数

```csharp
OllamaModel(string modelName = "llama2", string? baseUrl = null)
```

`baseUrl` 缺省自动探测本机 Ollama（默认 `http://localhost:11434`）。

## 最小示例

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.Ollama;

var model = new OllamaModel("llama3");

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
using AgentScope.Core.Model.Ollama;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new OllamaModel("llama3"))
    .Build();
```

## 流式调用

`OllamaModel` 继承 `OpenAIModel`，实现 `IStreamingChatModel`：

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## 注意事项

- 确保本机已安装并启动 Ollama，且目标模型已拉取（`ollama pull llama3`）。
- `OllamaModel` 构造函数仅暴露 `modelName` 和 `baseUrl`。如需自定义 formatter 或生成参数，使用 `new OpenAIModel(modelName, baseUrl: yourUrl)` 方式。
- 默认模型名为 `"llama2"`，建议显式指定。

完整接口说明见 [模型](../../docs/building-blocks/model.md)。
