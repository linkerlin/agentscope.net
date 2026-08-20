---
title: "计划模式"
description: "PlanModeManager、plan_mode 工具与中间件注入"
---

## 概述

计划模式（Plan Mode）把智能体分为两个工作状态：

| 模式 | 行为 |
|------|------|
| `PlanMode.Plan` | 只读规划：先制定计划再动手，系统提示词附加规划指令 |
| `PlanMode.Build` |（默认）正常执行 |

## PlanModeManager

```csharp
using AgentScope.Harness.Workspace;

var manager = new PlanModeManager();          // 默认 Build 模式

manager.SetMode(PlanMode.Plan);               // 进入计划模式
PlanMode current = manager.CurrentMode;       // 读取当前模式

manager.Toggle();                             // Plan ↔ Build 切换
manager.OnModeChanged += mode => Console.WriteLine($"模式切换为 {mode}");
```

## 工具

`PlanModeTools` 提供两个工具工厂（`AgentScope.Harness.Tool`）：

```csharp
using AgentScope.Harness.Tool;

ITool toggle = PlanModeTools.CreateToggleTool(manager);   // "plan_mode_toggle"
ITool query  = PlanModeTools.CreateQueryTool(manager);    // "plan_mode_query"
```

- `plan_mode_toggle`：可选参数 `mode`（`Plan` / `Build`），省略则切换；
- `plan_mode_query`：无参数，返回 `当前模式: Plan/Build`。

模型通过这两个工具自主决定何时进入 / 退出计划模式。

## 中间件

`PlanModeMiddleware`（Order 400，`HarnessAgentBuilder.Build()` 自动装配）读取 `ctx.Items["plan_mode"]`（字符串 `"plan"` 或 `"build"`），当值为 `"plan"` 时在系统提示词末尾追加规划指令，约束模型先规划后执行。

## 与 Agent 集成

```csharp
var planMode = new PlanModeManager();

var toolkit = new Toolkit()
    .AddTool(PlanModeTools.CreateToggleTool(planMode))
    .AddTool(PlanModeTools.CreateQueryTool(planMode));

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithToolkit(toolkit)
    .WithMiddleware(new PlanModeMiddleware())
    .Build();
```

:::{note}
`PlanModeMiddleware` 在 Build 时已自动装配（无需显式添加）；上述 `WithMiddleware` 仅为示意。`PlanModeContextState`（`AgentScope.Core.State`）可用于把当前计划模式随会话状态持久化。
:::

## 相关文档

- [Harness 架构](./architecture.md)
- [上下文压缩](./compaction.md) —— 长任务中的上下文控制
