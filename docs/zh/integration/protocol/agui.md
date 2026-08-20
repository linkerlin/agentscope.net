# AG-UI

`AgentScope.Core.AgUI` 命名空间将 AgentScope 的流事件转换为 [AG-UI Protocol](https://github.com/ag-ui/ag-ui) 事件，让前端 UI 可以实时渲染 Agent 运行过程。

## 何时使用

- 需要将 AgentScope Agent 接入 AG-UI 兼容前端或自研 Chat UI。
- 需要以 SSE 流式输出文本、推理内容、工具调用等 AG-UI 事件。

核心类均在 `AgentScope.Core.AgUI` 下，无需额外 NuGet 包。

## 快速上手

```csharp
using AgentScope.Core.AgUI.Adapter;
using AgentScope.Core.AgUI.Model;

// 配置适配器
var config = new AguiAdapterConfig
{
    EnableReasoning = true,
    EmitTokenUsage = false,
    EmitToolCallArgs = true,
    DefaultAgentId = "default"
};

// 创建适配器
var adapter = new AguiAgentAdapter(agent, config);

// 构建输入
var input = new RunAgentInput(
    ThreadId: "thread-1",
    RunId: "run-1",
    Messages: new[] { AguiMessage.UserMessage("你好") });

// 获取事件流（可序列化为 SSE 发送给前端）
await foreach (var evt in adapter.RunAsync(input))
{
    var sseData = AguiEventEncoder.Encode(evt);
    // 写入 HTTP 响应流
}
```

## 核心 API

### AguiAgentAdapter

| 构造方法 | 说明 |
| --- | --- |
| `AguiAgentAdapter(IAgent agent, AguiAdapterConfig? config = null)` | 包装 Agent 并转换为 AG-UI 事件流 |

| 方法 | 说明 |
| --- | --- |
| `IAsyncEnumerable<AguiEvent> RunAsync(RunAgentInput input)` | 运行 Agent 并以异步流形式产出 AG-UI 事件 |

### AguiAdapterConfig

| 属性 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `ToolMergeMode` | `ToolMergeMode` | `FrontendOnly` | 工具合并模式 |
| `EmitStateEvents` | bool | `false` | 是否发射状态快照/增量事件 |
| `EmitToolCallArgs` | bool | `true` | 是否发射工具调用参数事件 |
| `EmitTokenUsage` | bool | `false` | 是否发射 Token 用量信息 |
| `EnableReasoning` | bool | `true` | 是否启用推理/思考事件 |
| `EmitRunFinishedAfterError` | bool | `true` | 错误后是否仍发射 `RunFinished` |
| `RunTimeout` | `TimeSpan?` | `null` | 单次运行超时 |
| `DefaultAgentId` | string | `"default"` | 默认 Agent 标识符 |
| `EmitSubagentEventsAsNative` | bool | `false` | 子 Agent 事件是否作为原生 AG-UI 事件发射 |

### AguiAgentRegistry

```csharp
var registry = new AguiAgentRegistry();
registry.Register("agent-1", myAgent);
registry.RegisterFactory("agent-2", () => new MyAgent());

var agent = registry.GetAgent("agent-1");
bool exists = registry.HasAgent("agent-1");
registry.Unregister("agent-1");
registry.Clear();
```

### AguiEventEncoder

| 方法 | 说明 |
| --- | --- |
| `Encode(AguiEvent)` | 编码为 SSE data 行：`data: {json}\n\n` |
| `EncodeToJson(AguiEvent)` | 仅返回 JSON 字符串 |
| `EncodeComment(string)` | 编码 SSE 注释 |
| `KeepAlive()` | 生成 SSE 保活信号 |

### AguiMessageConverter

双向转换 AG-UI 消息与 AgentScope `Msg`：

```csharp
var converter = new AguiMessageConverter();
var msg = converter.ToMsg(aguiMessage);
var aguiMsg = converter.ToAguiMessage(msg);
var msgs = converter.ToMsgList(runAgentInput);
```

### AguiToolConverter

```csharp
var tool = AguiToolConverter.ToAguiTool("search", "搜索工具", schema);
```

### RunAgentInput

```csharp
public sealed record RunAgentInput(
    string ThreadId,
    string RunId,
    IReadOnlyList<AguiMessage> Messages,
    IReadOnlyList<AguiTool>? Tools = null,
    IReadOnlyList<AguiContext>? Context = null,
    IReadOnlyDictionary<string, object>? State = null,
    IReadOnlyDictionary<string, string>? ForwardedProps = null,
    IReadOnlyList<AguiResume>? Resume = null);
```

`AguiMessage` 提供静态工厂方法：`UserMessage`、`AssistantMessage`、`SystemMessage`、`ToolMessage`。

### 事件映射

| AgentScope 事件 | AG-UI 事件 |
| --- | --- |
| `ActingStart` | `TextMessageStart` |
| `ActingChunk` | `TextMessageContent` |
| `ActingFinish` | `TextMessageEnd` |
| `ToolCallStart` | `ToolCallStart` |
| `ToolCallChunk` | `ToolCallArgs` |
| `ToolCallFinish` | `ToolCallEnd` |
| `ReasoningStart`（需启用 `EnableReasoning`） | `ReasoningStart` / `ReasoningMessageStart` |
| `ReasoningChunk` | `ReasoningMessageContent` |
| `ReasoningFinish` | `ReasoningMessageEnd` / `ReasoningEnd` |

## 工具合并模式

| `ToolMergeMode` | 行为 |
| --- | --- |
| `FrontendOnly` | 仅使用前端传入工具 |
| `AgentOnly` | 忽略前端传入工具 |
| `MergeFrontendPriority` | 合并两侧，同名时前端优先 |
