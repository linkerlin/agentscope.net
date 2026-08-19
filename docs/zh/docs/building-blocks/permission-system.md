---
title: "Permission System"
description: "精细控制 agent 可以执行哪些 tool、何时执行"
---

## 概述

Permission system（`AgentScope.Core.Permission`）拦截 agent 的每一次工具调用，给出三种决策之一：**允许（Allow）** 执行、**拒绝（Deny）** 执行，或者**询问用户（Ask）** 确认。

它把静态配置与动态运行时分析组合起来。三个组件共同决定结果：

- **Rules** —— 针对每个 tool 与命令的显式 allow / deny / ask 模式，最高优先级。规则有两种来源：在 `PermissionContextState` 中静态预配置，或在 ASK 提示中由用户接受**建议规则**而动态加入。建议规则由本次工具调用自动生成 —— 一旦接受，将来相同的调用便会被自动处理，不再询问。
- **Mode** —— 配置阶段设定的全局静态策略；决定所有不命中任何规则的调用的默认行为（例如 `Explore` 让 agent 进入只读；`DontAsk` 静默拒绝未命中的调用）。
- **Built-in Checks** —— 由 tool 自身在运行时基于真实输入做的动态分析（在 `ToolBase.CheckPermissions` 中实现）。这些是运行时检查而非预配置模式，因此**不可绕过**，不受 mode 或 rules 覆盖。

```{mermaid}
sequenceDiagram
    participant LLM
    participant PS as Permission System
    participant Tool
    participant User

    LLM->>PS: Tool Call
    Note over PS: Built-in Checks · Rules · Mode

    alt ALLOW
        PS->>Tool: execute
        Tool->>LLM: result
    else DENY
        PS->>LLM: denied
    else ASK + Suggestions
        PS->>User: ASK + Suggestions
        alt User approves
            User->>Tool: allow
            Tool->>LLM: result
            User-->>PS: accept suggested rule
        else User denies
            User->>PS: deny
            PS->>LLM: denied
        end
    end
```

:::{dropdown} 详细决策流程
```{mermaid}
flowchart TD
    A([Tool Call]) --> B{Deny Rules?}
    B -->|Match| DENY([DENY])
    B -->|No Match| C{Ask Rules?}
    C -->|Match| ASK1([ASK])
    C -->|No Match| D{Tool-Specific Checks}
    D -->|EXPLORE + write op| DENY
    D -->|Dangerous path| ASK2([ASK])
    D -->|Pass| E{Allow Rules?}
    E -->|Match| ALLOW([ALLOW])
    E -->|No Match| F{"ACCEPT_EDITS + safe file op?"}
    F -->|Yes| ALLOW
    F -->|No| G{"Read-only Bash command?"}
    G -->|Yes| ALLOW
    G -->|No| H{BYPASS mode?}
    H -->|Yes| ALLOW
    H -->|No| I{DONT_ASK mode?}
    I -->|Yes| DENY
    I -->|No| ASK3([ASK])
    ASK1 --> S[Generate Suggestions]
    ASK2 --> S
    ASK3 --> S
    S --> U{User Confirms?}
    U -->|Approve| ALLOW
    U -->|Deny| DENY
    U -->|Apply Rule| R[Update Context] --> ALLOW
    style DENY fill:#ff6b6b,color:#fff
    style ALLOW fill:#51cf66,color:#fff
    style ASK1 fill:#ffd43b,color:#333
    style ASK2 fill:#ffd43b,color:#333
    style ASK3 fill:#ffd43b,color:#333
```
:::

:::{note}
Deny 规则与危险路径检查是**不可绕过的** —— 即使在 `Bypass` 模式下也照常生效。
:::

## Permission Mode

`PermissionMode` 枚举（`AgentScope.Core.Permission.PermissionMode`）支持以下模式，分别适配不同的部署场景：

| Mode | 行为 | 适用场景 |
|------|------|----------|
| `Default` | 所有操作都需要显式规则或用户确认 | 最安全，推荐默认值 |
| `AcceptEdits` | 自动放行工作目录内的文件操作 | 用户在场的活跃开发 |
| `Explore` | 只读：放行读、拒绝所有写与命令 | 代码探索、规划 |
| `Bypass` | 放行一切（deny / ask 规则仍生效） | 完全可信的沙箱 |
| `DontAsk` | 把所有 ASK 转为 DENY | 无人值守 / 计划任务 |

可以在创建 agent 时通过 `PermissionContext(...)` 设置 mode：

::::{tab-set}
:::{tab-item} 初始化时配置
```csharp
using AgentScope.Core;
using AgentScope.Core.Permission;

PermissionContextState permCtx =
        PermissionContextState.Builder()
                .WithMode(PermissionMode.Default)
                .Build();

ReActAgent agent =
        ReActAgent.Builder()
                .WithName("my_agent")
                .WithSysPrompt("...")
                .WithModel(model)
                .WithPermissionContext(permCtx)
                .Build();
```
:::
:::{tab-item} ACCEPT_EDITS 配合工作目录
```csharp
using AgentScope.Core.Permission;

PermissionContextState permCtx =
        PermissionContextState.Builder()
                .WithMode(PermissionMode.AcceptEdits)
                .AddWorkingDirectory(
                        "/my/project",
                        new AdditionalWorkingDirectory("/my/project", "userSettings"))
                .Build();
```
:::
::::

## Permission Rule

`PermissionRule`（record）把某个 tool 与具体的调用模式映射到三种行为之一：`Allow`、`Deny`、`Ask`。

每条规则由下述字段组成。当权限引擎评估一条规则时，它会用 `RuleContent` 与实际调用入参调用该 tool 的 `MatchRule()` 方法，判断规则是否命中。

- **`ToolName` · `string` · *required*** — 规则适用的 tool 名：内置 `todo_write`，或任意自定义 tool 名。

- **`RuleContent` · `string | null` · *optional*** — 匹配模式 —— 语义随 `ToolName` 变化，由该 tool 的 `MatchRule()` 方法解释。`null` 表示对该 tool 的所有调用均匹配。

- **`Behavior` · `PermissionBehavior` · *required*** — `Allow`、`Deny`、`Ask` 或 `Passthrough`

- **`Source` · `string` · *required*** — 规则来源：`"userSettings"`、`"projectSettings"`、`"session"`、`"suggested"` 等。

### 配置规则

**初始化时** —— 通过 `PermissionContextState.Builder()` 把规则传入：

```csharp
using AgentScope.Core.Permission;

PermissionContextState permCtx =
        PermissionContextState.Builder()
                .WithMode(PermissionMode.Default)
                .AddAllowRule(
                        "safe_read",
                        new PermissionRule(
                                "safe_read", null, PermissionBehavior.Allow, "userSettings"))
                .AddAskRule(
                        "dangerous_delete",
                        new PermissionRule(
                                "dangerous_delete",
                                null,
                                PermissionBehavior.Ask,
                                "userSettings"))
                .AddDenyRule(
                        "drop_table",
                        new PermissionRule(
                                "drop_table", null, PermissionBehavior.Deny, "userSettings"))
                .Build();
```

**运行时通过建议规则** —— 当权限系统返回 ASK 时，会基于本次调用自动生成建议规则。把已接受的规则附在 `ConfirmResult.AcceptedRules` 中回传，agent 会自动写入引擎：

```csharp
using AgentScope.Core.Event;

// ASK 决策中包含基于本次调用生成的 SuggestedRules（位于 ToolUseBlock 上）。
// 接受建议时，把它放入结果即可：
ConfirmResult result =
        new ConfirmResult(
                /* confirmed = */ true,
                /* toolCall  = */ toolCall,
                /* rules     = */ toolCall.SuggestedRules);
```

完整可运行示例：`agentscope-examples/documentation/.../tool/PermissionContextExample.java`、`hitl/PermissionHITLExample.java`。

## Built-in Checks

每个 tool 都实现了一个 `CheckPermissions(toolInput, context)` 方法（位于 `ToolBase`），在运行时基于真实调用入参执行检查，返回 `Task<PermissionDecision>`。这些检查不可绕过 —— 无论 mode 或 rules 是什么，它们都生效。

`PermissionDecision` 提供四个静态构造方法：`Allow(message)` / `Deny(message)` / `Ask(message)` / `Passthrough(message)`。返回 `Passthrough` 表示「我不强加判断，交给引擎按 rules / mode 评估」。

自定义 tool 可以重写 `CheckPermissions()` 实现自己的检查逻辑：

```csharp
using AgentScope.Core.Permission;
using AgentScope.Core.Tool;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MyTool : ToolBase
{
    public MyTool()
        : base(
                ToolBase.Builder()
                        .WithName("MyTool")
                        .WithDescription("...")
                        .IsReadOnly(false))
    {
    }

    public override Task<PermissionDecision> CheckPermissions(
            Dictionary<string, object> toolInput, ToolExecutionContext context)
    {
        object target = toolInput.GetValueOrDefault("target");

        // 自定义安全检查：阻止操作生产资源
        if (target is string s && s.StartsWith("prod-"))
        {
            return Task.FromResult(
                    PermissionDecision.Ask("Operation targets production resource: " + s));
        }

        // 返回 Passthrough 让引擎继续按 rules / mode 评估
        return Task.FromResult(PermissionDecision.Passthrough("default"));
    }
}
```

### 危险路径保护

`ToolBase` 内置的危险路径列表通过 `ToolDangerousPathConstants` 维护，自定义 tool 可以在 `[Tool]` 注解上追加 `DangerousFiles` / `DangerousDirectories` 把额外路径并入受保护集合。命中后即使在 `Bypass` 模式下也会强制 ASK。

| 类别 | 默认受保护示例 |
|------|----------------|
| Shell 配置 | `.bashrc`、`.zshrc`、`.bash_profile`、`.profile` |
| Git 配置 | `.gitconfig`、`.gitmodules` |
| SSH | `.ssh/config`、`.ssh/authorized_keys`、`id_rsa`、`id_ed25519` |
| 凭证 | `.env`、`.env.local`、`.npmrc`、`.pypirc`、`.aws/credentials` |
| 目录 | `.git/`、`.ssh/`、`.aws/`、`.kube/` |

## 结合 HITL

当权限引擎对某个工具调用返回 ASK 决策时，agent 不会直接执行，而是暂停并返回一个 `GenerateReason.PermissionAsking` 的响应。返回的 `Msg` 中包含处于 `Asking` 状态的 `ToolUseBlock`，调用方据此向用户展示待确认的操作，收集决策后通过 `ConfirmResult` 恢复 agent。

### 交互流程

1. 配置 ASK 规则，标记需要人工确认的工具
2. Agent 遇到 ASK 工具时暂停，返回 `PermissionAsking`
3. 从返回的 `Msg` 中提取 `ToolUseBlock`（状态为 `Asking`），向用户展示
4. 构建 `ConfirmResult`，附在新消息的 metadata 中恢复 agent

```csharp
using AgentScope.Core;
using AgentScope.Core.Event;
using AgentScope.Core.Message;
using AgentScope.Core.Permission;
using System.Collections.Generic;

// 1. 配置权限：safe_read 自动放行，dangerous_delete 需要确认
PermissionContextState permCtx =
        PermissionContextState.Builder()
                .WithMode(PermissionMode.Default)
                .AddAllowRule(
                        "safe_read",
                        new PermissionRule(
                                "safe_read", null, PermissionBehavior.Allow, "policy"))
                .AddAskRule(
                        "dangerous_delete",
                        new PermissionRule(
                                "dangerous_delete", null, PermissionBehavior.Ask, "policy"))
                .Build();

ReActAgent agent =
        ReActAgent.Builder()
                .WithName("GuardedAgent")
                .WithSysPrompt("...")
                .WithModel(model)
                .WithToolkit(toolkit)
                .WithPermissionContext(permCtx)
                .Build();

// 2. 调用 agent
Msg result = await agent.CallAsync(new UserMessage("Delete /tmp/important.txt"));

// 3. 检查是否需要用户确认
if (result != null && result.GenerateReason == GenerateReason.PermissionAsking)
{
    // 从返回的 Msg 中提取待确认的 ToolUseBlock
    List<ToolUseBlock> askingTools =
            result.Content
                    .Where(b => b is ToolUseBlock)
                    .Cast<ToolUseBlock>()
                    .Where(t => t.State == ToolCallState.Asking)
                    .ToList();

    // 向用户展示
    askingTools.ForEach(t => Console.WriteLine("Pending: " + t.Name + " " + t.Input));

    // 4. 收集用户决策，构建 ConfirmResult 恢复 agent
    bool approved = AskUser();
    List<ConfirmResult> confirmResults =
            askingTools
                    .Select(t => new ConfirmResult(approved, t))
                    .ToList();

    var meta = new Dictionary<string, object>
    {
        { Msg.MetadataConfirmResults, confirmResults }
    };
    Msg resumeMsg =
            Msg.Builder()
                    .WithName("user")
                    .WithRole(MsgRole.User)
                    .WithTextContent(approved ? "approved" : "denied")
                    .WithMetadata(meta)
                    .Build();

    Msg finalResult = await agent.CallAsync(new List<Msg> { resumeMsg });
}
```

### 全部工具被拒绝

当用户在确认界面拒绝了本轮推理产出的**全部**工具调用时，agent 默认会继续下一轮推理 —— 此时模型只能看到 "Permission denied by user" 的工具结果，容易产生无效推理。

如果需要在这种场景下停止 agent，可以装备一个 `OnActing` middleware 观察 `AllToolsDeniedEvent` 并发出 `RequestStopEvent`。停止后 `Msg.GenerateReason` 返回 `AllToolsDenied`。

具体实现参见 [Middleware — 全部工具被拒绝时停止 agent](./middleware.md#全部工具被拒绝时停止-agent)。

### Streaming 模式

使用 `StreamEventsAsync()` 时，不需要从返回的 `Msg` 提取 `ToolUseBlock` —— 通过事件流直接获得 `RequireUserConfirmEvent`，它携带了待确认的工具调用列表：

```csharp
using AgentScope.Core.Event;
using AgentScope.Core.Message;
using System.Collections.Generic;

// 订阅事件流
List<ToolUseBlock> pendingTools = null;

await foreach (var evt in agent.StreamEventsAsync(new List<Msg> { new UserMessage("Delete /tmp/important.txt") }))
{
    if (evt is RequireUserConfirmEvent confirmEvent)
    {
        // 直接从事件中获取待确认的 ToolUseBlocks
        pendingTools = confirmEvent.ToolCalls;
        pendingTools.ForEach(t =>
                Console.WriteLine("Pending: " + t.Name + " " + t.Input));

        // 收集用户决策后，在下一次 call 时恢复
        // （存储 pending 列表，在后续 call 时使用）
    }
}

// 恢复方式与 blocking API 相同：构建 ConfirmResult 附在 metadata 中
List<ConfirmResult> confirmResults =
        pendingTools
                .Select(t => new ConfirmResult(true, t))
                .ToList();
var meta = new Dictionary<string, object>
{
    { Msg.MetadataConfirmResults, confirmResults }
};
Msg resumeMsg =
        Msg.Builder()
                .WithName("user")
                .WithRole(MsgRole.User)
                .WithTextContent("approved")
                .WithMetadata(meta)
                .Build();
await agent.CallAsync(new List<Msg> { resumeMsg });
```

如果使用 `StreamEventsAsync(new List<Msg> { resumeMsg })` 发起恢复，事件流会在恢复执行工具之前包含
`UserConfirmResultEvent`。使用它的 `ReplyId` 将本次接受的确认结果关联到之前的
`RequireUserConfirmEvent`；该事件只包含本次恢复消息携带的确认结果。

两种模式的区别：

| | Blocking `CallAsync()` | Streaming `StreamEventsAsync()` |
|---|---|---|
| 获取待确认工具 | 从返回的 `Msg.Content` 中筛选 `ToolUseBlock`（状态为 `Asking`） | 从 `RequireUserConfirmEvent.ToolCalls` 直接获取 |
| 恢复方式 | 相同：构建 `ConfirmResult` 附在 metadata 中发起新的 `CallAsync()` | 相同 |
| 适用场景 | REST API、简单同步服务 | WebSocket、SSE、实时 UI |

### 无人值守模式

在 CI 或定时任务等无人值守场景下，把 mode 设为 `DontAsk`，所有 ASK 决策会自动降级为 DENY：

```csharp
PermissionContextState headless =
        PermissionContextState.Builder()
                .WithMode(PermissionMode.DontAsk)
                .AddAllowRule(
                        "safe_read",
                        new PermissionRule(
                                "safe_read", null, PermissionBehavior.Allow, "policy"))
                .Build();
// ASK 规则命中时自动拒绝，不会阻塞等待
```

完整可运行示例：`agentscope-examples/documentation/.../hitl/PermissionHITLExample.java`。

## 常见配方

下面的示例展示了如何为常见部署场景配置 `PermissionContext`。每个配方把一种 mode 与一组规则结合，匹配特定的使用场景。

::::{tab-set}
:::{tab-item} 只读探索
```csharp
// Explore 模式：agent 可以自由调用只读工具，所有写工具会被自动拒绝。
PermissionContextState explore =
        PermissionContextState.Builder()
                .WithMode(PermissionMode.Explore)
                .Build();

ReActAgent explorer =
        ReActAgent.Builder()
                .WithName("explorer")
                .WithSysPrompt("...")
                .WithModel(model)
                .WithPermissionContext(explore)
                .Build();
```
:::
:::{tab-item} 无人值守自动化
```csharp
using AgentScope.Core.Permission;

PermissionContextState ci =
        PermissionContextState.Builder()
                .WithMode(PermissionMode.DontAsk)
                .AddAllowRule(
                        "deploy",
                        new PermissionRule(
                                "deploy", "staging", PermissionBehavior.Allow, "project"))
                .AddAllowRule(
                        "git_commit",
                        new PermissionRule(
                                "git_commit", null, PermissionBehavior.Allow, "project"))
                .Build();

ReActAgent ciAgent =
        ReActAgent.Builder()
                .WithName("ci_agent")
                .WithSysPrompt("...")
                .WithModel(model)
                .WithPermissionContext(ci)
                .Build();
// 只有显式放行的命令会执行；其余调用被静默拒绝
```
:::
:::{tab-item} 阻止危险命令
```csharp
PermissionContextState bypassWithDeny =
        PermissionContextState.Builder()
                .WithMode(PermissionMode.Bypass)
                .AddDenyRule(
                        "drop_table",
                        new PermissionRule(
                                "drop_table", null, PermissionBehavior.Deny, "userSettings"))
                .AddDenyRule(
                        "force_push",
                        new PermissionRule(
                                "force_push", null, PermissionBehavior.Deny, "userSettings"))
                .Build();
// 除显式拒绝的工具外，其余均放行（deny 规则不可绕过）
```
:::
::::
