---
title: "消息与事件"
description: "Msg / ContentBlock 消息模型与 Event / EventType 流式事件"
---

## 消息（Msg）

`Msg`（`AgentScope.Core.Message`）是智能体之间、智能体与模型之间传递的统一消息类型。

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Id` | `string` | 随机 GUID | 消息唯一标识 |
| `Name` | `string?` | null | 可选发送者名称 |
| `Role` | `string` | `"user"` | 角色：`system` / `user` / `assistant` / `tool`（`MsgRole` 枚举同名） |
| `Content` | `object?` | null | 内容：字符串或 `List<ContentBlock>` |
| `Url` | `List<string>?` | null | 附加 URL 列表 |
| `Timestamp` | `DateTime` | UtcNow | 创建时间 |
| `Metadata` | `Dictionary<string, object>?` | null | 扩展元数据 |

### 构造：Msg.Builder()

```csharp
using AgentScope.Core.Message;

Msg msg = Msg.Builder()
    .Id("msg-001")                       // 可选
    .Name("alice")                       // 可选
    .Role("user")                        // 默认 "user"
    .TextContent("帮我总结这份文档")       // 等价于 Content("纯文本")
    .Url(new List<string>())             // 可选
    .Metadata(new Dictionary<string, object>())   // 或用 AddMetadata(key, value) 逐条添加
    .Build();
```

MsgBuilder 全部方法：`Id` / `Name` / `Role` / `Content(object)` / `TextContent(string)` / `Url` / `Timestamp` / `Metadata` / `AddMetadata` / `Build`。

### 便捷子类

| 类 | 构造 | 说明 |
|----|------|------|
| `UserMessage` | `new UserMessage()` 或 `new UserMessage(name, content)` | Role 固定 `"user"`（**没有单参文本构造**，传文本请用 Builder） |
| `SystemMessage` / `AssistantMessage` / `ToolResultMessage` | 同上模式 | Role 分别固定 `system` / `assistant` / `tool` |

### 读取内容

```csharp
string? text = msg.GetTextContent();   // string 直接返回；块列表则拼接文本块
msg.SetTextContent("新内容");           // 覆盖为纯文本
string json = msg.ToString();          // JSON 序列化
```

## ContentBlock 体系

多模态内容以 `ContentBlock` record（同一命名空间）表示，放入 `List<ContentBlock>` 后作为 `Msg.Content`。所有块都用 record 对象初始化器构造（没有 Builder）：

| 块类型 | `Type` 值 | 必填字段 | 说明 |
|--------|-----------|----------|------|
| `TextBlock` | `"text"` | `Text` | 文本 |
| `ImageBlock` | `"image"` | `Url`（或 `Data` 字节） | 图片 |
| `AudioBlock` | `"audio"` | `Url`，可选 `DurationSec` | 音频 |
| `VideoBlock` | `"video"` | `Url`，可选 `PosterUrl` | 视频 |
| `ToolUseBlock` | `"tool_use"` | `Id`、`Name`，可选 `Input` | 模型发起的工具调用 |
| `ToolResultBlock` | `"tool_result"` | `Id`，可选 `Output`、`IsError` | 工具执行结果，`ExtractText()` 提取文本 |
| `ThinkingBlock` | `"thinking"` | `Thinking`，可选 `Signature` | 模型思考过程 |

```csharp
var msg = Msg.Builder()
    .Role("user")
    .Content(new List<ContentBlock>
    {
        new TextBlock { Text = "这张图里是什么？" },
        new ImageBlock { Url = "https://example.com/cat.png", MimeType = "image/png" }
    })
    .Build();
```

## 事件（Event 与 EventType）

`StreamEventsAsync` 产出 `Event`（`AgentScope.Core.Events`，注意与细粒度 `AgentEvent` record 层次区分，见下文）：

```csharp
public class Event
{
    public EventType Type { get; }                          // 事件类型
    public Msg? Message { get; }                            // 关联消息（可为 null）
    public bool IsLast { get; }                             // 是否为本流最后一个事件
    public IReadOnlyDictionary<string, object> Metadata { get; }

    // 便捷判断：IsReasoning / IsToolCall / IsActing / IsSummary / IsError
    public static Event ErrorEvent(Msg? message, string? errorMessage = null, bool isLast = true);
}
```

### EventType 枚举

| 类别 | 枚举值 |
|------|--------|
| 推理 | `ReasoningStart` / `ReasoningChunk` / `ReasoningFinish` |
| 工具调用 | `ToolCallStart` / `ToolCallChunk` / `ToolCallFinish` |
| 行动 | `ActingStart` / `ActingChunk` / `ActingFinish` |
| 摘要 | `SummaryStart` / `SummaryChunk` / `SummaryFinish` |
| 错误 | `Error` |

### 消费示例

```csharp
using AgentScope.Core.Events;

await foreach (Event evt in agent.StreamEventsAsync(userMsg))
{
    switch (evt.Type)
    {
        case EventType.ReasoningChunk:
            Console.Write(evt.Message?.GetTextContent());
            break;
        case EventType.ToolCallStart:
            Console.WriteLine("\n[工具调用开始]");
            break;
        case EventType.Error:
            Console.WriteLine($"\n[错误] {evt.Metadata.GetValueOrDefault("error")}");
            break;
    }
    if (evt.IsLast) break;
}
```

## 细粒度 AgentEvent record 层次

`AgentScope.Core.Events` 中还存在一组细粒度事件 record（公共抽象基类 `AgentEvent(string ReplyId)`），主要由 A2A / AgUI 等协议适配层使用：

| 事件 | 载荷 | 说明 |
|------|------|------|
| `AgentStartEvent` / `AgentEndEvent` | `AgentName`、`SessionId?` | Agent 生命周期 |
| `AgentResultEvent` | `Msg Result` | 最终结果 |
| `TextBlockStartEvent` / `TextBlockDeltaEvent` / `TextBlockEndEvent` | `Text`（Delta） | 文本块流式 |
| `ThinkingBlockStartEvent` / `ThinkingBlockDeltaEvent` / `ThinkingBlockEndEvent` | `Thinking`（Delta） | 思考块流式 |
| `ToolCallEvent` / `ToolResultEvent` | `ToolUseBlock` / `ToolResultBlock` | 工具调用与结果 |
| `RequireUserConfirmEvent` | `ToolName`、`Arguments?` | 需要用户确认（HITL） |
| `ExceedMaxItersEvent` / `AllToolsDeniedEvent` | `MaxIterations` | 循环终止异常 |
| `HintBlockEvent` | `Hint` | 非交互提示 |
| `ModelCallStartEvent` / `ModelCallEndEvent` | `ModelName?` | 模型调用边界 |
| `CustomAgentEvent` | `Name`、`Value?` | 自定义扩展 |

`Events/AdditionalEvents.cs` 中另有：`UserConfirmResultEvent`（携带 `ConfirmResult`）、`RequestStopEvent`、`DataBlockStart/Delta/EndEvent`（二进制数据流，Base64 Delta）、`SubagentExposedEvent`、`RequireExternalExecutionEvent` / `ExternalExecutionResultEvent`。

:::{note}
`EnhancedReActAgent.StreamEventsAsync` 产出的是上一节的粗粒度 `Event`；`AgentEvent` record 层次用于协议适配与更精细的 UI 事件建模。两者都在 `AgentScope.Core.Events` 命名空间下。
:::

## 相关文档

- [智能体](./agent.md) —— 谁产出事件、如何消费
- [模型](./model.md) —— 模型层的 `ChatResponse` 流式块
