---
title: "Plan Mode"
description: "Think before acting: a read-only phase that writes a plan file and requires HITL approval before executing"
---

## Role

Plan Mode lets the agent "figure out and write down intent" before executing. While active, the agent is in a **read-only phase**:

- Only **read-only tools** plus 4 whitelisted tools work: `plan_enter` / `plan_write` / `plan_exit` / `todo_write` (the shell can be opted in — see [below](#allowing-the-shell-during-the-plan-phase-opt-in)).
- Any other tool call is rejected immediately (the agent sees a "plan-mode denied" note).
- Exiting Plan Mode requires HITL confirmation (reusing the permission system's ASK), so the model can't unilaterally jump into execution.

This pipeline encodes "design → plan → human review → execute" — combined with `todo_write` and subagents, it noticeably reduces "improvise-then-break-things" outcomes on long tasks.

## Opt-in

```csharp
HarnessAgent agent = HarnessAgent.Builder()
    .Name("planner")
    .Model(model)
    .Workspace(workspace)
    .EnablePlanMode()                          // installs the Plan Mode trio
    .PlanFileDirectory("plans")                // optional; default "plans"
    .Build();
```

Builder options:

| Method | Default | Notes |
|--------|---------|-------|
| `EnablePlanMode()` / `EnablePlanMode(bool)` | `false` | enable Plan Mode |
| `PlanFileDirectory(string)` | `"plans"` | plan-file root (workspace-relative) |
| `AllowShellInPlanMode()` / `AllowShellInPlanMode(bool)` | `false` | opt in to running the shell (`execute`) during the plan phase — see [Allowing the shell during the plan phase](#allowing-the-shell-during-the-plan-phase-opt-in) |

You can also call `EnableTaskList()` so that todos created during the plan phase show up as a small reminder before each reasoning step.

## The three tools

| Tool | Purpose | Params |
|------|---------|--------|
| `plan_enter` | Enter Plan Mode | none |
| `plan_write` | Write content to the current plan file (default `plans/PLAN.md`) | `content` |
| `plan_exit` | Exit Plan Mode → execution phase; HITL confirmation | `rationale` (optional) |

`plan_write` is **a dedicated write entry for Plan Mode** — avoids the security risk of whitelisting the generic `write_file` (which would let the model write anywhere during plan).

## Workflow

```{mermaid}
sequenceDiagram
    autonumber
    participant U as User
    participant A as Agent
    participant H as Human (HITL)
    participant FS as workspace

    U->>A: "Refactor module X for me"
    A->>A: plan_enter
    A->>A: think → call read_file / grep_files (read-only)
    A->>FS: plan_write to plans/PLAN.md
    A->>H: plan_exit → HITL confirmation
    H-->>A: ConfirmResult(true)
    A->>A: enter execution phase; all tools allowed
```

Any non-whitelisted tool call (e.g. `write_file`, or `execute` unless you [opt in](#allowing-the-shell-during-the-plan-phase-opt-in)) during the plan phase is rejected immediately with something like:

```text
[Tool denied — plan mode is active]
Only read-only tools and plan_enter / plan_write / plan_exit / todo_write are allowed.
```

Seeing the denial, the model naturally switches back to "write the plan first".

## Reading the outcome

Plan-mode entry is autonomous, so a run can end in four states. `IsPlanModeActive() == false` alone is ambiguous — don't treat it as success without checking whether planning actually happened:

| Terminal state | Meaning |
|----------------|---------|
| Never entered plan mode | Model chose to work directly in build mode — a valid decision, often because the task doesn't match the workspace. |
| Entered → `plan_exit` | Success: planned, got approval, now in build mode. |
| Still in plan mode + `PLAN.md` exists | Drafted a plan but didn't exit; resume the session to approve. |
| Still in plan mode + no `PLAN.md` | "Narrate but don't act": the final message may *read* like a plan but none was written — give more specific input or a matching codebase. |

To tell these apart programmatically, track whether `plan_enter` / `plan_write` were called (e.g. from `ToolCallStartEvent`) alongside the final `IsPlanModeActive()` and the plan file's existence.

## Allowing the shell during the plan phase (opt-in)

By default the shell tool (`execute`) is **denied** during the plan phase. The shell is *dual-use*: a single tool call can read (`cat` / `ls` / `grep` / `git log`) or mutate (`rm` / `>` / `git commit` / `npm install`), and Plan Mode decides what to permit purely by **tool name** — so it cannot tell a read invocation from a write one. Denying the shell keeps the read-only guarantee intact.

But shell access is often the most flexible way to investigate a codebase and produce a *realistic* plan. When you accept that trade-off, opt in:

```csharp
HarnessAgent agent = HarnessAgent.Builder()
    .Name("planner")
    .Model(model)
    .Workspace(workspace)
    .EnablePlanMode()
    .AllowShellInPlanMode()   // let the model run the shell read-only during plan
    .Build();
```

With the opt-in enabled:

- `execute` is added to the plan-phase allow-list, so the model can investigate via the shell.
- The plan banner gains an extra instruction telling the model to keep shell usage **read-only** (`cat` / `ls` / `grep` / `git log/diff/show/status`) and **not** to run mutating commands until the plan is approved.
- The dedicated file-editing tools (`write_file` / `edit_file`) **remain denied** — they are the primary mutation path, so the read-only intent is still enforced for file writes.

This mirrors how OpenCode handles its plan agent: it allows the shell for investigation, hard-blocks the edit/write tools, and relies on the prompt to keep the shell read-only. The guarantee is therefore *softer* than the default (the model could still mutate via the shell), so prefer enabling this together with a **sandboxed filesystem** to contain the blast radius.

## Runtime permission switching (the "bypass" escape hatch)

Plan Mode is one specific phase switch. Underneath it, every session carries a [`PermissionMode`](../building-blocks/context.md) that the permission engine evaluates against. You can flip that mode at runtime — for example to grant a deliberate, user-initiated "skip all permission prompts" toggle (similar to a YOLO / dangerous-skip switch in other coding tools):

```csharp
RuntimeContext ctx = RuntimeContext.Builder().SessionId("my-session").Build();

agent.SetPermissionMode(ctx, PermissionMode.BYPASS);    // allow everything, no prompts
// ... run the operations that need full access ...
agent.SetPermissionMode(ctx, PermissionMode.DEFAULT);   // restore normal enforcement

PermissionMode current = agent.GetPermissionMode(userId, sessionId);
```

`SetPermissionMode(...)` preserves the session's configured allow/deny/ask rules and working directories — only the mode changes — and rebuilds that session's cached permission engine so the switch takes effect on the **next** call. An in-flight call keeps the engine it started with.

⚠ `BYPASS` disables all rule evaluation, so treat it as an explicit, per-session, opt-in action and prefer pairing it with a sandbox. To run unattended without prompts but *with* enforcement, use `PermissionMode.DONT_ASK` instead (ASK decisions become DENY rather than being auto-allowed).

## Plan state is persisted

Plan Mode is **runtime state** and is auto-persisted along with `AgentState` — process restarts, node failovers, and cross-replica restores all bring back the plan phase. The plan file itself is written to `plans/` in the workspace and goes through whichever filesystem mode you've configured (local / sandbox / remote KV), so it's distributed-safe.

## Programmatic enter/exit

When app code drives Plan Mode (e.g. an admin console button):

```csharp
RuntimeContext ctx = RuntimeContext.Builder().SessionId("my-session").Build();
agent.EnterPlanMode(ctx);    // equivalent to the LLM calling plan_enter
agent.ExitPlanMode(ctx);     // equivalent to plan_exit; programmatic entry does NOT trigger HITL
agent.IsPlanModeActive(ctx);
```

If you use `agentscope-admin-aspnet-starter`, the admin HTTP API also exposes Plan Mode controls (`POST /v1/admin/sessions/{id}:enter-plan-mode` / `:exit-plan-mode` / `GET /v1/admin/sessions/{id}/plan`).

## Interaction with subagents

⚠ Current **known gap**: subagents spawned via `agent_spawn` during Plan Mode **do not automatically inherit the read-only restriction**. To restrict the child:

- Narrow `tools` in the child's declaration to a read-only set, or
- Also `EnablePlanMode()` on the child's own builder and enter it explicitly

A future release will propagate plan-mode restrictions parent → child automatically.

## Interaction with `todo_write`

Plan Mode and `todo_write` (provided by core) are **independent but commonly used together**:

- **Plan Mode** — phase switch + plan file + HITL exit
- **`todo_write`** — maintain a structured "what to do now" list during execution (whole-list replace; exactly one `in_progress`)

Typical workflow: write `PLAN.md` during the plan phase → `plan_exit` → in execution use `todo_write` to slice the PLAN into 5–8 todos → progress one at a time. Each reasoning step shows the agent a todos reminder to stay focused.

⚠ Don't confuse with subagent **background tasks** (`task_output` / `task_cancel` / `task_list`) — that's a different concept; see [Subagent](./subagent.md).

## Viewing the task list

The task list lives in `AgentState.TasksContext` and is persisted automatically with every `Call()`. To read it from application code:

```csharp
List<Task> tasks = agent.GetAgentState(userId, sessionId)
        .TasksContext
        .Tasks;

foreach (Task t in tasks)
{
    Console.WriteLine("[{0}] {1}", t.State, t.Subject);
    // state: PENDING / IN_PROGRESS / COMPLETED
}
```

If you use `agentscope-admin-aspnet-starter`, the admin REST API provides a ready-made endpoint:

```
GET /v1/admin/sessions/{sessionId}/tasks
```

It returns each task's subject, state, owner, and dependency info (`blocks` / `blockedBy`).

To observe task changes in real time through the event stream, listen for `todo_write` tool calls in `StreamEvents()`:

```csharp
agent.StreamEvents(message)
    .Where(e => e.Type == AgentEventType.TOOL_RESULT_END)
    .Where(e => "todo_write".Equals(((ToolResultEndEvent) e).ToolCallName))
    .Subscribe(e =>
    {
        // Re-read the latest task list from state
        var tasks = agent.GetAgentState(userId, sessionId)
                .TasksContext.Tasks;
        UpdateUI(tasks);
    });
```

## Related Pages

- [Workspace](./workspace.md) — `plans/` directory location
- [Subagent](./subagent.md) — `todo_write` ≠ subagent task; don't confuse them
- [Architecture](./architecture.md) — where Plan Mode sits in the call() timeline
