---
title: "智能体"
description: "了解如何在 AgentScope .NET 2.0 中定义和配置智能体"
---

## 概述

智能体接口位于 `AgentScope.Core.Agent` 命名空间，默认实现是 **`EnhancedReActAgent`**（`AgentScope.Core.EnhancedReActAgent`）——一个推理-行动（ReAct）循环引擎，把模型、工具、权限、Hook、记忆和事件整合到统一接口中。

:::{warning}
旧类 `ReActAgent` 已整体标记 `[Obsolete]`，请使用 `EnhancedReActAgent`。两者共享相近的 Builder 方法，但 `EnhancedReActAgent` 额外支持 Hook、权限引擎、状态持久化策略、HITL 确认回调等能力。
:::

### 核心接口

| 接口 | 方法 | 说明 |
|------|------|------|
| `IAgent` / `ICallableAgent` | `CallAsync(IReadOnlyList<Msg>, RuntimeContext?)` → `Task<Msg>` | 运行推理-行动循环并返回最终消息 |
| `IStreamableAgent` | `StreamEventsAsync(Msg, RuntimeContext?)` → `IAsyncEnumerable<Event>` | 同 `CallAsync`，但流式产出 `Event`（见[消息与事件](./message-and-event.md)） |
| `IAgent` | `ObserveAsync(Msg)` / `ObserveAsync(IReadOnlyList<Msg>)` | 触发一次回复（`EnhancedReActAgent` 中等价于 `CallAsync`） |
| `IInterruptible` | `Interrupt()` / `Interrupt(Msg)` | 中断当前正在执行调用 |
| `IStructuredOutputCapableAgent` | `GenerateStructuredOutputAsync<T>(IEnumerable<Msg>)` → `Task<T>` | 按 C# 类型约束模型输出 JSON 并反序列化 |
| `IStateModule` | `SaveTo / LoadFrom / LoadIfExists(Session, sessionKey)` | 把状态保存到 / 从 `Session` 恢复（见[上下文与 AgentState](./context.md)） |

`HarnessAgent`（`AgentScope.Harness`）实现 `IAgent`，在内部组合 `EnhancedReActAgent` 与各 Harness 子系统，详见 [Harness 架构](../harness/architecture.md)。

## 构建智能体

通过 `EnhancedReActAgentBuilder` 创建智能体。**模型是必填项**，未设置时 `Build()` 抛出 `InvalidOperationException`。

```csharp
using AgentScope.Core;
using AgentScope.Core.Model;
using AgentScope.Core.Tool;

EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Name("my_agent")                                    // 默认 "EnhancedReActAgent"
    .SysPrompt("你是一个有帮助的助手。")                  // 默认内置提示词
    .Model(new DashScopeModel("qwen-plus", apiKey))      // 必填
    .Memory(new MemoryBase())                            // 可选；默认 new MemoryBase()
    .MaxIterations(10)                                   // 默认 10
    .AddTool(new CalculatorTool())                       // 可选；可多次调用
    .Build();
```

### Builder 方法全表

| 方法 | 参数 | 默认值 | 说明 |
|------|------|--------|------|
| `Name(string)` | Agent 名称 | `"EnhancedReActAgent"` | 用于日志与事件 |
| `Model(IModel)` | 模型实例 | **必填** | 见[模型](./model.md) |
| `SysPrompt(string)` | 系统提示词 | 内置默认 | 运行期可通过 `agent.SystemPrompt` 属性读写 |
| `Memory(IMemory)` | 记忆实现 | `new MemoryBase()` | 见[上下文与 AgentState](./context.md) |
| `AddTool(ITool)` | 单个工具 | — | 可多次调用，见[工具](./tool.md) |
| `ToolGroupManager(ToolGroupManager)` | 工具分组管理器 | null | 启用工具分组激活/停用 |
| `AddToolGroup(ToolGroup)` | 注册一个分组 | — | 自动创建 `ToolGroupManager` |
| `MaxIterations(int)` | 最大迭代次数 | `10` | ReAct 主循环上限 |
| `StatePersistence(StatePersistence)` | 状态持久化策略 | `StatePersistence.All` | 控制 Memory/Toolkit 是否随 Session 持久化 |
| `HookManager(HookManager)` | Hook 管理器 | `new HookManager()` | 见下文 |
| `PermissionEngine(IPermissionEngine)` | 权限引擎 | null | 见[权限系统](./permission-system.md) |
| `Verbose(bool)` | 控制台详细日志 | `false` | 输出每次迭代过程 |
| `ConfirmCallback(Func<RequireUserConfirmEvent, Task<ConfirmResult>>)` | HITL 确认回调 | null（回退控制台询问） | 工具被权限系统标记 Ask 时回调 |
| `AutoApproveOnAsk(bool)` | 无终端时自动放行 | `false` | `Console.IsInputRedirected` 且无回调时生效 |
| `Build()` | — | — | 构建实例 |

> 旧 `ReActAgentBuilder`（`Name/Model/SysPrompt/Memory/AddTool/Tools/ToolGroupManager/AddToolGroup/MaxIterations`）仍随 `ReActAgent` 一起提供但已废弃，方法名没有 `With` 前缀，与新 Builder 一致。

## 运行智能体

### CallAsync

```csharp
using AgentScope.Core.Message;

Msg result = await agent.CallAsync(
    Msg.Builder().Role("user").TextContent("当前目录有哪些文件？").Build());
Console.WriteLine(result.GetTextContent());
```

`HarnessAgent` 额外提供单条 `Msg`、`string` 文本两个便捷重载；`EnhancedReActAgent` 的 `CallAsync` 接受 `IReadOnlyList<Msg>`，取最后一条作为本轮用户输入。

### StreamEventsAsync

```csharp
using AgentScope.Core.Events;

await foreach (Event evt in agent.StreamEventsAsync(userMsg))
{
    if (evt.Type == EventType.ReasoningChunk)
        Console.Write(evt.Message?.GetTextContent());
    if (evt.IsLast) break;
}
```

事件模型（`Event` + `EventType` 枚举）的完整说明见[消息与事件](./message-and-event.md)。

### 结构化输出

`GenerateStructuredOutputAsync<T>` 在提示中注入 JSON 约束，把模型输出反序列化为指定类型；解析失败抛出 `ModelException`：

```csharp
public record WeatherResponse(string Location, string Temperature, string Condition);

WeatherResponse weather = await agent.GenerateStructuredOutputAsync<WeatherResponse>(
    new[] { Msg.Builder().Role("user").TextContent("旧金山天气如何？").Build() });
Console.WriteLine(weather.Temperature);
```

也有流式版本 `StreamStructuredOutputAsync<T>(messages, StreamOptions)`，最终以一个 `ReasoningFinish` 事件携带 JSON 文本。

## 多用户 / 多会话

每次调用传入的 `RuntimeContext`（record，`AgentScope.Core.Agent`）携带 `UserId` / `SessionId`：

```csharp
RuntimeContext ctx = RuntimeContext.Empty
    .WithUserId("alice")
    .WithSessionId("session-1");

await agent.CallAsync(msg, ctx);
```

`RuntimeContext` 通过 `AsyncLocal` 流转（`RuntimeContext.Current` 可在整个异步链路读取），中间件、工具都能拿到同一份引用。会话历史的持久化与恢复依赖 `Memory` 配置，见[上下文与 AgentState](./context.md)。

## 中断执行（Interrupt）

```csharp
agent.Interrupt();                 // 中断当前执行
agent.Interrupt(interruptMsg);     // 带一条消息中断
```

中断是实例级的：`EnhancedReActAgent` 在每次迭代前检查取消标记，被中断后保存状态并返回部分结果。

## Hook 体系

Hook 在推理 / 行动 / 摘要各阶段前后被回调，与 Agent 主流程解耦：

```csharp
using AgentScope.Core.Hook;

class LoggingHook : HookBase     // HookBase 提供全部虚默认实现
{
    public override Task OnPreReasoningAsync(PreReasoningEvent evt)
    {
        Console.WriteLine($"[{Name}] 即将推理，上下文 {evt.Context.Length} 字符");
        return Task.CompletedTask;
    }

    public override Task OnPostActingAsync(PostActingEvent evt)
    {
        Console.WriteLine($"[{Name}] 动作 {evt.Action} 成功={evt.ActionSuccess}");
        return Task.CompletedTask;
    }
}

var hooks = new HookManager();
hooks.RegisterHook(new LoggingHook());

EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(model)
    .HookManager(hooks)
    .Build();
```

`IHook` 全部 11 个回调：`OnPreReasoningAsync` / `OnPostReasoningAsync` / `OnPreActingAsync` / `OnPostActingAsync` / `OnPreSummaryAsync` / `OnPostSummaryAsync` / `OnReasoningChunkAsync` / `OnActingChunkAsync` / `OnSummaryChunkAsync` / `OnErrorAsync`，以及 `Name` 属性。任何 Hook 把事件上的 `ShouldStop` 置为 `true` 会终止后续处理。

## 人机交互（HITL）

当配置了权限引擎且某次工具调用被判定为 `Ask` 时，`EnhancedReActAgent` 会：

1. 若设置了 `ConfirmCallback`，调用它并等待 `ConfirmResult`（`ConfirmResult.Approve()` / `ConfirmResult.Deny(reason)`）；
2. 否则回退到控制台交互（`y/N` 询问）；无交互终端时按 `AutoApproveOnAsk` 决定放行或拒绝。

```csharp
EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(model)
    .PermissionEngine(new PermissionEngine())
    .ConfirmCallback(async confirmEvent =>
    {
        Console.WriteLine($"工具 {confirmEvent.ToolName} 请求执行，参数 {confirmEvent.Arguments}");
        return Console.ReadLine() == "y"
            ? ConfirmResult.Approve()
            : ConfirmResult.Deny("用户拒绝");
    })
    .Build();
```

权限规则的配置见[权限系统](./permission-system.md)。

## 状态保存与恢复

`EnhancedReActAgent` 实现 `IStateModule`，把自身状态（元信息、记忆消息、工具组激活状态）存入 `Session.Context` 字典：

```csharp
using AgentScope.Core.Session;

var sessionManager = new SessionManager();
Session session = sessionManager.CreateSession(name: "demo");

// 调用后保存
await agent.CallAsync(msg);
agent.SaveTo(session, "main");            // 写入 AgentMetaState / 记忆 / ToolkitState

// 进程重启后：重建 agent，从同一 Session 恢复
agent.LoadIfExists(session, "main");      // 不存在时静默跳过；LoadFrom 则抛异常
```

`StatePersistence` record 控制持久化范围：`StatePersistence.All`（默认）/ `StatePersistence.None` / `new StatePersistence(MemoryManaged: true, ToolkitManaged: false, PlanNotebookManaged: true)`。

完整机制与 `IAgentStateStore` 生态见[上下文与 AgentState](./context.md)。

## 延伸阅读

- [模型](./model.md) —— 各提供商模型构造与流式接口
- [工具](./tool.md) —— `[Tool]` 注册、Toolkit、MCP
- [权限系统](./permission-system.md) —— 工具调用三态决策
- [Harness 架构](../harness/architecture.md) —— `HarnessAgentBuilder` 完整装配
