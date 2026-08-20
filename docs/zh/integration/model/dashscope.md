# DashScope 模型

`AgentScope.Core.Model.DashScope.DashScopeModel` 接入阿里云 DashScope 通义千问系列模型。

## 构造函数

```csharp
DashScopeModel(string modelName, string? apiKey = null, string? baseUrl = null,
    DashScopeChatFormatter? formatter = null, GenerateOptions? defaultOptions = null)
```

常量：`DefaultBaseUrl = "https://dashscope.aliyuncs.com"`、`ChatEndpoint = "/compatible-mode/v1/chat/completions"`。

## 最小示例

```csharp
using AgentScope.Core.Model;
using AgentScope.Core.Model.DashScope;

string key = System.Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY");
var model = new DashScopeModel("qwen-plus", key);

string response = await model.GenerateAsync(
    new ModelRequest
    {
        Messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("Explain AgentScope").Build()
        }
    }).Result.Text;
```

## Agent 集成

```csharp
using AgentScope.Core.Model.DashScope;
using AgentScope.Core.Agent;

var agent = new EnhancedReActAgentBuilder()
    .Model(new DashScopeModel("qwen-plus", key))
    .Build();
```

## 流式调用

`DashScopeModel` 实现了 `IStreamingChatModel`：

```csharp
await foreach (ChatResponse chunk in model.GenerateStreamAsync(messages))
{
    Console.Write(chunk.Content);
}
```

## 注意事项

- `apiKey` 缺省时建议从 `DASHSCOPE_API_KEY` 环境变量读取。
- 端点默认使用 OpenAI 兼容格式（`/compatible-mode/v1/chat/completions`）。
- 自定义 `DashScopeChatFormatter` 或 `GenerateOptions` 可通过构造函数传入。

完整接口说明见 [模型](../../docs/building-blocks/model.md)。
