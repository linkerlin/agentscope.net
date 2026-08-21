---
title: "权限系统"
description: "IPermissionEngine / PermissionEngine 规则与 HITL 确认"
---

## 概述

权限系统（`AgentScope.Core.Permission`）在**每次工具执行前**做三态决策：

```csharp
public enum PermissionBehavior { Allow, Deny, Ask, Passthrough }
public enum PermissionMode { Default, AcceptEdits, Explore, Bypass, DontAsk }

public interface IPermissionEngine
{
    PermissionDecision Evaluate(ToolCallRequest request);
}

public record PermissionRule(string Pattern, PermissionBehavior Behavior);
public record PermissionDecision(
    PermissionBehavior Behavior,
    string Reason,
    List<string>? SuggestedRules = null,
    Dictionary<string, object>? UpdatedInput = null);
public class ToolCallRequest
{
    public string ToolName { get; set; }
    public Dictionary<string, object>? Arguments { get; set; }
}
```

## PermissionEngine

内置实现 `PermissionEngine` 采用 6 步优先级状态机（deny > ask > 内置放行 > allow > bypass > 默认 ask）：

```csharp
var permission = new PermissionEngine(PermissionMode.Default)
    .AddRule("calculator", PermissionBehavior.Allow)          // 精确名 / 通配符模式
    .AddRule("shell_command*", PermissionBehavior.Ask)        // shell 系列需确认
    .AddRule("write_file", PermissionBehavior.Deny);          // 禁止写文件

PermissionDecision decision = permission.Evaluate(
    new ToolCallRequest { ToolName = "shell_command", Arguments = new() { ["command"] = "ls" } });
// decision.Behavior == PermissionBehavior.Ask
```

说明：

- `CalculatorTool` 与 `GetTimeTool` 在状态机第 3 步**自动放行**（代码中通过 C# 类名匹配）；
- `PermissionMode.Bypass` 模式下未命中 deny/ask 规则的调用直接放行；
- `AddRule` 返回自身，支持链式调用。

## 与 Agent 集成

通过 `EnhancedReActAgentBuilder.PermissionEngine(...)` 注入；`HarnessAgentBuilder` 侧用 `WithPermission(...)`：

```csharp
EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(model)
    .PermissionEngine(new PermissionEngine()
        .AddRule("shell_command*", PermissionBehavior.Ask))
    .ConfirmCallback(async confirmEvent =>
    {
        Console.WriteLine($"工具 {confirmEvent.ToolName} 请求执行");
        return Console.ReadLine() == "y"
            ? ConfirmResult.Approve()
            : ConfirmResult.Deny("用户拒绝");
    })
    .Build();
```

决策行为：

| 决策 | 行为 |
|------|------|
| `Allow` | 直接执行 |
| `Deny` | 返回 `ActionResult.ToolCall(..., success: false, "权限拒绝: ...")`，模型看到错误结果 |
| `Ask` | 触发 HITL：有 `ConfirmCallback` 则回调；否则控制台 `y/N` 询问；无交互终端时按 `AutoApproveOnAsk(true)` 放行或拒绝 |

## 权限状态持久化

`PermissionContextState` record（`Mode`、`WorkingDirectory`、`AllowRules`、`DenyRules`、`AskRules`）是权限引擎的状态快照，可随 `Session` 序列化，用于跨进程恢复权限规则。另配有 `AdditionalWorkingDirectory(Path, Source)` record 表达附加可写目录。

## 相关文档

- [智能体 — 人机交互](./agent.md#人机交互hitl)
- [工具](./tool.md) —— 工具定义与执行
