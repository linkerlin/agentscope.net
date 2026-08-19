---
title: "消息与事件"
description: "智能体通信，与前端流式数据传输"
---

消息（Message）与事件（Event）是 AgentScope 中两种基础数据结构。

- **消息** — 智能体间通信与持久化的基本单元。每个 `Msg` 代表一个完整的对话轮次，存储在上下文中并在智能体之间传递。
- **事件** — 前端交互与流式传输的基本单元。事件携带增量进度更新（文本 token、工具调用片段、权限请求等），驱动实时界面和人工介入工作流。

单次 `CallAsync` 调用产生的事件序列最终汇聚成恰好一条 assistant `Msg`，这保证了完整的消息状态始终可以从事件流中还原。

## 消息

`Msg`（位于 `AgentScope.Core.Message`）代表对话中的一个轮次——用户输入、智能体回复或系统指令，内容以有序的类型化块（`ContentBlock`）列表表示。

:::{tip}
一条 assistant 消息对应智能体一次完整的 `CallAsync` 周期（反复推理和执行，直到产出最终回复）。
:::

### 结构

`Msg` 类的核心字段如下：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | `string` | 唯一消息标识符 |
| `Name` | `string` | 发送方名称（可空） |
| `Role` | `MsgRole` | `User` / `Assistant` / `System` / `Tool` |
| `Content` | `List<ContentBlock>` | 有序内容块列表（不可变） |
| `Metadata` | `Dictionary<string, object>` | 任意键值元数据 |
| `Timestamp` | `string` | 创建时间（`yyyy-MM-dd HH:mm:ss.SSS`） |
| `Usage` | `ChatUsage` | Token 用量（仅 assistant 消息） |
| `GenerateReason` | `GenerateReason` | 退出原因：`ModelStop` / `ToolSuspended` / `ReasoningStopRequested` / `ActingStopRequested` / `AllToolsDenied` / `Interrupted` / `MaxIterations` |

### 内容块

消息内容由类型化的块组成，每种块代表一类独立信息。块类位于 `AgentScope.Core.Message`：

| 块类型 | 说明 | 允许出现在 |
|--------|------|-----------|
| `TextBlock` | 纯文本内容 | User、Assistant、System |
| `DataBlock` | 二进制数据（图片、音频、视频），通过 base64 或 URL；统一替代旧的 ImageBlock/AudioBlock/VideoBlock | User、Assistant |
| `ImageBlock` / `AudioBlock` / `VideoBlock` | 旧版具体多媒体块（仍兼容，新代码建议用 `DataBlock`） | User |
| `ThinkingBlock` | 模型推理过程（思维链） | Assistant |
| `ToolUseBlock` | 工具调用，包含 `Id` / `Name` / `Input` / `State`（`ToolCallState`） | Assistant |
| `ToolResultBlock` | 工具执行结果，包含 `State`（`ToolResultState`） | Assistant |
| `HintBlock` | 以用户上下文形式注入循环的指令 | Assistant |

:::{note}
角色约束在构造时强制执行：`User` 消息只能包含 text/data/image/audio/video 块；`System` 消息只能包含 `TextBlock`；`Assistant` 消息可包含所有块类型。
:::

### 创建消息

按 role 固定的子类提供便捷构造（`AgentScope.Core.Message.UserMessage` / `AssistantMessage` / `SystemMessage` / `ToolResultMessage`）。当 content 是普通字符串时，会自动包装为 `TextBlock`。

```csharp
using AgentScope.Core.Message;

// 用户消息 —— 文本
UserMessage userText = new UserMessage("user", "这张图片里有什么？");

// 多模态用户消息
UserMessage userMulti =
        new UserMessage(
                "user",
                TextBlock.Builder().WithText("描述这张图片：").Build(),
                DataBlock.Builder()
                        .WithSource(Base64Source.Builder()
                                .WithData("...")
                                .WithMediaType("image/png")
                                .Build())
                        .Build());

// 系统消息 —— 仅文本
SystemMessage systemMsg = new SystemMessage("system", "你是一个有用的助手。");

// 助手消息 —— 允许所有块类型
AssistantMessage assistantMsg = new AssistantMessage("agent", "结果如下...");
```

需要更多可选字段（`Metadata`、`Timestamp`、`Usage`、`GenerateReason`）时使用各子类的 `Builder()`：

```csharp
UserMessage msg =
        UserMessage.Builder()
                .WithName("user")
                .WithTextContent("Hello")
                .Build();
```

### 访问内容

`Msg` 提供了一组辅助方法用于提取特定块类型：

| 方法 | 返回值 |
|------|--------|
| `TextContent` | 所有 `TextBlock` 的拼接文本（按 `\n` 连接），无文本块时返回空字符串 |
| `GetContentBlocks<T>()` | 按类型过滤后的块列表 |
| `GetFirstContentBlock<T>()` | 首个匹配类型的块，无则返回 null |
| `HasContentBlocks<T>()` | 若存在指定类型的块则返回 `true` |

```csharp
using AgentScope.Core.Message;

// 获取所有文本内容
string text = msg.TextContent;

// 获取所有工具调用
List<ToolUseBlock> toolCalls = msg.GetContentBlocks<ToolUseBlock>();

// 检查消息是否包含工具结果
if (msg.HasContentBlocks<ToolResultBlock>())
{
    // ...
}
```

## 事件

事件是消息的流式对应物。智能体执行过程中会持续产出一系列 `AgentEvent` 对象（位于 `AgentScope.Core.Event`），表示增量进度——文本 token 到达、工具调用逐步构建、结果流式返回。每个事件都是轻量且自包含的。

### 事件生命周期

每个事件都携带 `ReplyId`，将其关联到正在构建的消息。在一次回复中，`BlockId` 或 `ToolCallId` 用作事件关联键，表示事件属于同一个内容块生命周期。事件遵循 **start → delta → end** 模式：

```{mermaid}
sequenceDiagram
    participant Client
    participant Agent

    Agent->>Client: AgentStartEvent

    rect rgba(100, 150, 255, 0.1)
        Note over Client,Agent: 推理阶段
        Agent->>Client: ModelCallStartEvent
        rect rgba(200, 200, 100, 0.1)
            Note over Client,Agent: TextBlock (blockId)
            Agent->>Client: TextBlockStartEvent
            Agent->>Client: TextBlockDeltaEvent (×N)
            Agent->>Client: TextBlockEndEvent
        end
        rect rgba(200, 200, 100, 0.1)
            Note over Client,Agent: DataBlock (blockId)
            Agent->>Client: DataBlockStartEvent
            Agent->>Client: DataBlockDeltaEvent (×N)
            Agent->>Client: DataBlockEndEvent
        end
        rect rgba(200, 200, 100, 0.1)
            Note over Client,Agent: ToolUseBlock (toolCallId)
            Agent->>Client: ToolCallStartEvent
            Agent->>Client: ToolCallDeltaEvent (×N)
            Agent->>Client: ToolCallEndEvent
        end
        Agent->>Client: ModelCallEndEvent
    end

    rect rgba(100, 255, 150, 0.1)
        Note over Client,Agent: 执行阶段
        rect rgba(200, 200, 100, 0.1)
            Note over Client,Agent: ToolResultBlock (toolCallId)
            Agent->>Client: ToolResultStartEvent
            Agent->>Client: ToolResultTextDeltaEvent (×N)
            Agent->>Client: ToolResultDataDeltaEvent (×N)
            Agent->>Client: ToolResultEndEvent
        end
    end

    Agent->>Client: AgentEndEvent
```

同一次回复中的所有事件共享相同的 `ReplyId`。在回复内部，用 `BlockId` 关联文本/思考/数据块事件，用 `ToolCallId` 关联工具调用和工具结果事件。`BlockId` 是 `ReplyId` 作用域内的关联键，不要求是全局唯一的随机 ID；当某类内容块在一次回复中最多出现一个生命周期时，实现可以使用稳定的类型标识（如文本块的固定标识）作为 `BlockId`。

### 事件类型

所有事件继承自 `AgentEvent`（位于 `AgentScope.Core.Event`），提供以下公共成员：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | `string` | 唯一事件标识符 |
| `CreatedAt` | `string` | ISO 8601 时间戳 |
| `Type` | `AgentEventType` | 事件类型枚举 |
| `Source` | `string` | 事件来源路径。顶层 Agent 为 `null`；子 Agent 事件为斜杠分隔的路径（如 `"main/researcher"`），用于区分父子 Agent 事件 |
| `Metadata` | `Dictionary<string, object>` | 可选键值元数据。远程子 agent 转发时会写入 `TaskId`（`AgentEvent.MetadataTaskId`，对应 harness / Agent Protocol 任务 id）与 `ParentSessionId`（`AgentEvent.MetadataParentSessionId`，对应父 session） |

事件按类别分组如下。除特别说明外，每个事件还携带 `ReplyId`，关联到正在构建的消息。

  :::{dropdown} 生命周期事件
**AgentStartEvent** — 智能体开始新的回复。

    | 属性 | 类型 | 说明 |
    |------|------|------|
    | `ReplyId` | `string` | 回复消息 ID |
    | `SessionId` | `string` | 会话 ID |
    | `Name` | `string` | 智能体名称 |
    | `Role` | `string` | 智能体角色（默认 `"assistant"`） |

    **AgentEndEvent** — 智能体完成回复。

    | 属性 | 类型 | 说明 |
    |------|------|------|
    | `ReplyId` | `string` | 回复消息 ID |

    **ExceedMaxItersEvent** — 智能体达到最大推理-执行迭代次数。

    | 属性 | 类型 | 说明 |
    |------|------|------|
    | `ReplyId` | `string` | 回复消息 ID |

    **RequestStopEvent** — 中间件或工具发起的提前停止请求。
:::

  :::{dropdown} 文本流式事件
**TextBlockStartEvent** — 新的文本块开始。

    | 属性 | 类型 | 说明 |
    |------|------|------|
    | `ReplyId` | `string` | 回复消息 ID |
    | `BlockId` | `string` | 文本块在当前回复中的关联键 |

    **TextBlockDeltaEvent** — 增量文本内容到达。

    | 属性 | 类型 | 说明 |
    |------|------|------|
    | `ReplyId` | `string` | 回复消息 ID |
    | `BlockId` | `string` | 文本块在当前回复中的关联键 |
    | `Delta` | `string` | 增量文本内容 |

    **TextBlockEndEvent** — 文本块完成。

    | 属性 | 类型 | 说明 |
    |------|------|------|
    | `ReplyId` | `string` | 回复消息 ID |
    | `BlockId` | `string` | 文本块在当前回复中的关联键 |
:::

  :::{dropdown} 思考流式事件
**ThinkingBlockStartEvent / ThinkingBlockDeltaEvent / ThinkingBlockEndEvent** —— 与文本流式事件结构对应，仅用于模型的思维链内容；`BlockId` 同样表示当前回复中的关联键。
:::

  :::{dropdown} 数据流式事件
**DataBlockStartEvent / DataBlockDeltaEvent / DataBlockEndEvent** —— 与文本流式事件结构对应，承载图片 / 音频 / 视频等二进制数据：

    - `DataBlockStartEvent`：`MediaType` 返回 MIME 类型（如 `"image/png"`）。
    - `DataBlockDeltaEvent`：`Data` 返回增量 base64 编码数据。
:::

  :::{dropdown} 工具调用流式事件
**ToolCallStartEvent** — 智能体开始一次工具调用。

    | 属性 | 类型 | 说明 |
    |------|------|------|
    | `ReplyId` | `string` | 回复消息 ID |
    | `ToolCallId` | `string` | 工具调用唯一标识符 |
    | `ToolCallName` | `string` | 被调用的工具名称 |

    **ToolCallDeltaEvent** — 增量工具调用参数到达；`Delta` 返回 JSON 参数片段。

    **ToolCallEndEvent** — 工具调用参数完成。
:::

  :::{dropdown} 工具结果流式事件
**ToolResultStartEvent** — 工具开始执行（带 `ToolCallId`、`ToolCallName`）。

    **ToolResultTextDeltaEvent** — 工具的增量文本输出；`Delta` 返回文本片段。

    **ToolResultDataDeltaEvent** — 工具的二进制数据输出；与 `DataBlockDeltaEvent` 类似，包含 `MediaType` / `Data` / `Url` 字段。

    **ToolResultEndEvent** — 工具执行完成。

    | 属性 | 类型 | 说明 |
    |------|------|------|
    | `ReplyId` | `string` | 回复消息 ID |
    | `ToolCallId` | `string` | 对应工具调用的 ID |
    | `State` | `ToolResultState` | 最终状态：`Success`、`Error`、`Interrupted`、`Denied`、`Running` |
:::

  :::{dropdown} 模型调用事件
**ModelCallStartEvent** — 模型 API 调用开始（带 `ModelName`）。

    **ModelCallEndEvent** — 模型 API 调用完成（带 `InputTokens` / `OutputTokens`）。
:::

  :::{dropdown} 人工介入事件
**RequireUserConfirmEvent** — 智能体暂停等待用户确认。

    | 属性 | 类型 | 说明 |
    |------|------|------|
    | `ReplyId` | `string` | 回复消息 ID |
    | `ToolCalls` | `List<ToolUseBlock>` | 待用户确认的工具调用列表 |

    **RequireExternalExecutionEvent** — 智能体暂停等待外部执行。

    | 属性 | 类型 | 描述 |
    |------|------|------|
    | `ReplyId` | `string` | 回复消息 ID |
    | `ToolCalls` | `List<ToolUseBlock>` | 待外部执行的工具调用列表 |

    **UserConfirmResultEvent** — 用户提供确认结果。携带 `List<ConfirmResult>`。
     `ReplyId` 与最初暂停智能体的 `RequireUserConfirmEvent` 相同。

    | 属性 | 类型 | 描述 |
    |------|------|------|
    | `ReplyId` | `string` | 关联的 `RequireUserConfirmEvent` 的回复 ID |
    | `ConfirmResults` | `List<ConfirmResult>` | 本次恢复接受的确认结果 |

    **ExternalExecutionResultEvent** — 后续 `CallAsync()` 恢复外部执行暂停时发出。
     携带一个或多个 `ToolResultBlock`，且 `ReplyId` 与之前的 `RequireExternalExecutionEvent` 相同。

    | 属性 | 类型 | 说明 |
    |------|------|------|
    | `ReplyId` | `string` | 关联的 `RequireExternalExecutionEvent` 的回复 ID |
    | `ToolResults` | `List<ToolResultBlock>` | 本次恢复接受的外部执行结果 |

    **AllToolsDeniedEvent** — 用户通过 HITL 确认拒绝了最近一轮推理产出的全部工具调用。该事件通过 `OnActing` middleware 链发出，middleware 可据此发出 `RequestStopEvent` 停止 agent。若无 middleware 处理，agent 默认继续下一轮推理（向后兼容）。

    | 属性 | 类型 | 说明 |
    |------|------|------|
    | `DeniedToolCalls` | `List<ToolUseBlock>` | 被拒绝的工具调用列表 |
:::

  :::{dropdown} 子 Agent 事件
**SubagentExposedEvent** — 通过 `agent_spawn(expose_to_user=true)` 生成的子 Agent 被暴露为用户可寻址的入口点。SSE / 流式消费端可据此在 UI 上渲染新的会话入口。

| 属性 | 类型 | 说明 |
|------|------|------|
| `SubagentId` | `string` | 子 Agent 的唯一标识 |
| `AgentId` | `string` | 子 Agent 的 agent 类型 ID |
| `SessionId` | `string` | 子 Agent 的会话 ID |
| `Label` | `string` | 用户可见的标签名（可选） |
:::

## 从事件流重建消息

事件与消息并非相互独立，而是同一数据的两种视图。`StreamEventsAsync` 产出的事件流可以按 `ReplyId` / `BlockId` / `ToolCallId` 聚合还原成完整的 `AssistantMessage`。这保证了最终消息状态可以仅凭事件流完整还原。

可以参考 `AgentScope.Core` 中的 `Agent/StreamingHook.cs` 与 `agentscope-examples/documentation/.../streaming/AgentEventStreamExample.java`，它们演示了用 LINQ 算子按 block 分组并累积内容的标准做法。

```csharp
using AgentScope.Core.Event;
using System.Text;

StringBuilder accumulated = new StringBuilder();

await foreach (var evt in agent.StreamEventsAsync(userMsg))
{
    if (evt is AgentStartEvent start)
    {
        Console.WriteLine("[start replyId=" + start.ReplyId + "]");
    }
    else if (evt is TextBlockDeltaEvent delta)
    {
        accumulated.Append(delta.Delta);
    }
    else if (evt is ToolCallStartEvent tc)
    {
        Console.WriteLine("[tool] " + tc.ToolCallName);
    }
    else if (evt is ToolResultEndEvent end)
    {
        Console.WriteLine("[tool result state=" + end.State + "]");
    }
    else if (evt is AgentEndEvent)
    {
        Console.WriteLine("\n[end] full text:\n" + accumulated);
    }
}
```

:::{tip}
这种设计让部署更加灵活：后端可以通过 SSE 把事件流推给前端，前端在客户端侧重建消息。即使连接中断，从任意检查点重放事件序列也能精确恢复消息状态。
:::

### 示例：流式界面

构建流式界面的典型模式（ASP.NET Core SSE 形态可参考 `streaming/StreamingWebExample.cs`）：

```csharp
using AgentScope.Core.Event;
using AgentScope.Core.Message;

await foreach (var evt in agent.StreamEventsAsync(new UserMessage("user", "帮我修复这个 bug")))
{
    if (evt is AgentStartEvent start)
    {
        Console.WriteLine("[start replyId=" + start.ReplyId + "]");
    }
    else if (evt is TextBlockDeltaEvent delta)
    {
        Console.Write(delta.Delta);
    }
    else if (evt is ToolCallStartEvent tc)
    {
        Console.WriteLine("\n[正在调用 " + tc.ToolCallName + "...]");
    }
    else if (evt is ToolResultEndEvent end)
    {
        Console.WriteLine("[工具执行完成：" + end.State + "]");
    }
    else if (evt is AgentEndEvent)
    {
        Console.WriteLine("\n[完成]");
    }
}
```

## 延伸阅读

::::{grid} 2

:::{grid-item-card} 智能体
:link: ./agent.html

智能体如何在 ReAct 循环中产出事件和消息
:::
  :::{grid-item-card} 上下文
:link: context.html

消息如何存储与持久化
:::

::::
