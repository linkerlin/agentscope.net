---
title: "Plan Mode"
description: "PlanModeManager, plan_mode tools, and middleware injection"
---

## Overview

Plan Mode divides the agent into two working states:

| Mode | Behavior |
|------|------|
| `PlanMode.Plan` | Read-only planning: make a plan before acting, system prompt augmented with planning instructions |
| `PlanMode.Build` | (default) Normal execution |

## PlanModeManager

```csharp
using AgentScope.Harness.Workspace;

var manager = new PlanModeManager();          // default Build mode

manager.SetMode(PlanMode.Plan);               // enter plan mode
PlanMode current = manager.CurrentMode;       // read current mode

manager.Toggle();                             // Plan ↔ Build toggle
manager.OnModeChanged += mode => Console.WriteLine($"Mode switched to {mode}");
```

## Tools

`PlanModeTools` provides two tool factories (`AgentScope.Harness.Tool`):

```csharp
using AgentScope.Harness.Tool;

ITool toggle = PlanModeTools.CreateToggleTool(manager);   // "plan_mode_toggle"
ITool query  = PlanModeTools.CreateQueryTool(manager);    // "plan_mode_query"
```

- `plan_mode_toggle`: optional parameter `mode` (`Plan` / `Build`), toggles if omitted;
- `plan_mode_query`: no parameters, returns `Current mode: Plan/Build`.

The model autonomously decides when to enter/exit plan mode via these two tools.

## Middleware

`PlanModeMiddleware` (Order 400, auto-assembled by `HarnessAgentBuilder.Build()`) reads `ctx.Items["plan_mode"]` (string `"plan"` or `"build"`). When the value is `"plan"`, it appends planning instructions to the end of the system prompt, constraining the model to plan before acting.

## Integration with Agent

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
`PlanModeMiddleware` is auto-assembled at Build time (no explicit addition needed); the `WithMiddleware` above is for illustration only. `PlanModeContextState` (`AgentScope.Core.State`) can be used to persist the current plan mode with session state.
:::

## Related Documentation

- [Harness Architecture](./architecture.md)
- [Context Compaction](./compaction.md) —— Context control in long tasks
