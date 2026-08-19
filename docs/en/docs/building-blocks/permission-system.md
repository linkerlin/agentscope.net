---
title: "Permission System"
description: "Fine-grained control over which tools your agents can execute and when"
---

## Overview

The permission system (`AgentScope.Core.Permission`) intercepts every tool call the agent makes and produces one of three decisions: **ALLOW**, **DENY**, or **ASK** (request user confirmation).

It combines static configuration with dynamic runtime analysis. Three components together decide the outcome:

- **Rules** — explicit allow / deny / ask patterns per tool and command, with the highest priority. Rules come from two sources: static configuration in `PermissionContextState`, or **suggested rules** added dynamically when the user accepts them at an ASK prompt. Suggested rules are auto-generated from the current invocation — once accepted, identical future calls are auto-handled without prompting.
- **Mode** — a global static policy set at configuration time; decides the default behaviour for calls that match no rule (e.g. `EXPLORE` makes the agent read-only, `DONT_ASK` silently denies anything not matching a rule).
- **Built-in Checks** — runtime analysis performed by the tool itself based on the actual input (implemented in `ToolBase.CheckPermissions`). These are runtime checks rather than preconfigured patterns, so they are **non-bypassable** — they are not subject to mode or rules.

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

:::{dropdown} Detailed decision flow
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
Deny rules and dangerous-path checks are **non-bypassable** — they apply even in `BYPASS` mode.
:::

## Permission Mode

The `PermissionMode` enum (`AgentScope.Core.Permission.PermissionMode`) supports the following modes:

| Mode | Behaviour | Use case |
|------|-----------|----------|
| `DEFAULT` | All operations require explicit rules or user confirmation | Safest default, recommended |
| `ACCEPT_EDITS` | Auto-allow file ops inside the working directory | Active development with the user present |
| `EXPLORE` | Read-only: allow reads, deny all writes and commands | Code exploration, planning |
| `BYPASS` | Allow everything (deny / ask rules still apply) | Fully trusted sandbox |
| `DONT_ASK` | Demote ASK to DENY | Unattended / scheduled runs |

Set the mode on the agent builder via `PermissionContext(...)`:

::::{tab-set}
:::{tab-item} Initial config
```csharp
using AgentScope.Core;
using AgentScope.Core.Permission;

PermissionContextState permCtx =
        PermissionContextState.Builder()
                .Mode(PermissionMode.DEFAULT)
                .Build();

ReActAgent agent =
        ReActAgent.Builder()
                .Name("my_agent")
                .SysPrompt("...")
                .Model(model)
                .PermissionContext(permCtx)
                .Build();
```
:::
:::{tab-item} ACCEPT_EDITS with working dir
```csharp
using AgentScope.Core.Permission;

PermissionContextState permCtx =
        PermissionContextState.Builder()
                .Mode(PermissionMode.ACCEPT_EDITS)
                .AddWorkingDirectory(
                        "/my/project",
                        new AdditionalWorkingDirectory("/my/project", "userSettings"))
                .Build();
```
:::
::::

## Permission Rule

`PermissionRule` (a record) maps a tool plus a specific call pattern to one of three behaviours: `ALLOW`, `DENY`, `ASK`.

Each rule has the fields below. When the engine evaluates a rule, it calls the tool's `MatchRule()` with the `RuleContent` and the actual input to decide whether the rule fires.

- **`ToolName` · `string` · *required*** — The tool name the rule applies to: `todo_write` (built-in) or any custom tool name.

- **`RuleContent` · `string | null` · *optional*** — Match pattern — semantics depend on the tool, interpreted by the tool's `MatchRule()`. `null` means the rule matches every invocation of that tool.

- **`Behavior` · `PermissionBehavior` · *required*** — `ALLOW`, `DENY`, `ASK`, or `PASSTHROUGH`

- **`Source` · `string` · *required*** — Origin of the rule: `"userSettings"`, `"projectSettings"`, `"session"`, `"suggested"`, …

### Configuring rules

**At init time** — pass rules through `PermissionContextState.Builder()`:

```csharp
using AgentScope.Core.Permission;

PermissionContextState permCtx =
        PermissionContextState.Builder()
                .Mode(PermissionMode.DEFAULT)
                .AddAllowRule(
                        "safe_read",
                        new PermissionRule(
                                "safe_read", null, PermissionBehavior.ALLOW, "userSettings"))
                .AddAskRule(
                        "dangerous_delete",
                        new PermissionRule(
                                "dangerous_delete",
                                null,
                                PermissionBehavior.ASK,
                                "userSettings"))
                .AddDenyRule(
                        "drop_table",
                        new PermissionRule(
                                "drop_table", null, PermissionBehavior.DENY, "userSettings"))
                .Build();
```

**At runtime via suggested rules** — when the permission system returns ASK, it auto-generates suggested rules based on the current invocation. Pass the accepted rules in `ConfirmResult` and the agent will write them into the engine:

```csharp
using AgentScope.Core.Event;

// ASK decisions carry suggestedRules on the ToolUseBlock.
// Accept them by attaching to the result:
ConfirmResult result =
        new ConfirmResult(
                /* confirmed = */ true,
                /* toolCall  = */ toolCall,
                /* rules     = */ toolCall.GetSuggestedRules());
```

Runnable examples: `agentscope-examples/documentation/.../tool/PermissionContextExample.cs`, `hitl/PermissionHITLExample.cs`.

## Built-in checks

Every tool implements `CheckPermissions(toolInput, context)` (on `ToolBase`) — a runtime check on the actual input that returns `Task<PermissionDecision>`. These checks cannot be bypassed: they apply regardless of mode or rules.

`PermissionDecision` provides four static factories: `Allow(message)` / `Deny(message)` / `Ask(message)` / `Passthrough(message)`. Returning `PASSTHROUGH` means "I'm not deciding — let the engine evaluate rules and mode."

A custom tool can override `CheckPermissions()` for its own logic:

```csharp
using AgentScope.Core.Permission;
using AgentScope.Core.Tool;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MyTool : ToolBase
{
    public MyTool()
        : base(
                ToolBase.Builder()
                        .Name("MyTool")
                        .Description("...")
                        .ReadOnly(false))
    {
    }

    public override Task<PermissionDecision> CheckPermissions(
            Dictionary<string, object> toolInput, ToolExecutionContext context)
    {
        object target = toolInput.GetValueOrDefault("target");

        // Custom safety check: block production resources.
        if (target is string s && s.StartsWith("prod-"))
        {
            return Task.FromResult(
                    PermissionDecision.Ask("Operation targets production resource: " + s));
        }

        // Return PASSTHROUGH to let the engine continue evaluating rules / mode.
        return Task.FromResult(PermissionDecision.Passthrough("default"));
    }
}
```

### Dangerous-path protection

The `ToolBase` dangerous-path list is maintained in `ToolDangerousPathConstants`. A custom tool can append more paths via the `DangerousFiles` / `DangerousDirectories` attributes on `[Tool]`. Once matched, the path triggers ASK even in `BYPASS` mode.

| Category | Examples |
|----------|----------|
| Shell config | `.bashrc`, `.zshrc`, `.bash_profile`, `.profile` |
| Git config | `.gitconfig`, `.gitmodules` |
| SSH | `.ssh/config`, `.ssh/authorized_keys`, `id_rsa`, `id_ed25519` |
| Credentials | `.env`, `.env.local`, `.npmrc`, `.pypirc`, `.aws/credentials` |
| Directories | `.git/`, `.ssh/`, `.aws/`, `.kube/` |

## HITL integration

When the permission engine returns an ASK decision for a tool call, the agent pauses instead of executing and returns a response with `GenerateReason.PERMISSION_ASKING`. The returned `Msg` contains the `ToolUseBlock`s in `ASKING` state. The caller extracts them, presents the pending operation to the user, and resumes the agent with `ConfirmResult` objects.

### Interaction flow

1. Configure ASK rules for tools that require human confirmation
2. Agent pauses on ASK tools, returning `PERMISSION_ASKING`
3. Extract `ToolUseBlock`s (with `ASKING` state) from the returned `Msg` and show them to the user
4. Build `ConfirmResult` objects and attach them to the resume message via metadata

```csharp
using AgentScope.Core;
using AgentScope.Core.Event;
using AgentScope.Core.Message;
using AgentScope.Core.Permission;
using System;
using System.Collections.Generic;
using System.Linq;

// 1. Configure permissions: safe_read auto-allowed, dangerous_delete requires confirmation
PermissionContextState permCtx =
        PermissionContextState.Builder()
                .Mode(PermissionMode.DEFAULT)
                .AddAllowRule(
                        "safe_read",
                        new PermissionRule(
                                "safe_read", null, PermissionBehavior.ALLOW, "policy"))
                .AddAskRule(
                        "dangerous_delete",
                        new PermissionRule(
                                "dangerous_delete", null, PermissionBehavior.ASK, "policy"))
                .Build();

ReActAgent agent =
        ReActAgent.Builder()
                .Name("GuardedAgent")
                .SysPrompt("...")
                .Model(model)
                .Toolkit(toolkit)
                .PermissionContext(permCtx)
                .Build();

// 2. Call the agent
Msg result = await agent.CallAsync(new UserMessage("Delete /tmp/important.txt"));

// 3. Check whether user confirmation is needed
if (result != null && result.GetGenerateReason() == GenerateReason.PERMISSION_ASKING)
{
    // Extract the ASKING ToolUseBlocks from the returned Msg
    List<ToolUseBlock> askingTools =
            result.GetContent()
                    .Where(b => b is ToolUseBlock)
                    .Cast<ToolUseBlock>()
                    .Where(t => t.GetState() == ToolCallState.ASKING)
                    .ToList();

    // Show pending operations to the user
    askingTools.ForEach(t => Console.WriteLine("Pending: " + t.GetName() + " " + t.GetInput()));

    // 4. Collect the user's decision, build ConfirmResult, and resume
    bool approved = AskUser();
    List<ConfirmResult> confirmResults =
            askingTools
                    .Select(t => new ConfirmResult(approved, t))
                    .ToList();

    var meta = new Dictionary<string, object>
    {
        { Msg.METADATA_CONFIRM_RESULTS, confirmResults }
    };
    Msg resumeMsg =
            Msg.Builder()
                    .Name("user")
                    .Role(MsgRole.USER)
                    .TextContent(approved ? "approved" : "denied")
                    .Metadata(meta)
                    .Build();

    Msg finalResult = await agent.CallAsync(new List<Msg> { resumeMsg });
}
```

### All tools denied

When the user denies **all** tool calls from a reasoning step in the confirmation UI, the agent continues to the next reasoning iteration by default — the model only sees "Permission denied by user" tool results, which often leads to unhelpful reasoning.

To stop the agent in this scenario, wire up an `OnActing` middleware that observes `AllToolsDeniedEvent` and emits a `RequestStopEvent`. After stopping, `Msg.GetGenerateReason()` returns `ALL_TOOLS_DENIED`.

See [Middleware — Stop agent when all tools are denied](./middleware.md#stop-agent-when-all-tools-are-denied) for the implementation.
### Streaming mode

When using `StreamEvents()`, you don't need to extract `ToolUseBlock`s from the returned `Msg` — the event stream delivers a `RequireUserConfirmEvent` that carries the pending tool calls directly:

```csharp
using AgentScope.Core.Event;
using AgentScope.Core.Message;
using System;
using System.Collections.Generic;
using System.Linq;

// Subscribe to the event stream
await foreach (var evt in agent.StreamEvents(new List<Msg> { new UserMessage("Delete /tmp/important.txt") }))
{
    if (evt is RequireUserConfirmEvent confirmEvent)
    {
        // Get pending ToolUseBlocks directly from the event
        List<ToolUseBlock> pending = confirmEvent.GetToolCalls();
        pending.ForEach(t =>
                Console.WriteLine("Pending: " + t.GetName() + " " + t.GetInput()));

        // Collect user decision, store pending list for the resume call
        pendingTools = pending;
    }
}

// Resume is the same as with the blocking API: build ConfirmResult in metadata
List<ConfirmResult> confirmResults =
        pendingTools
                .Select(t => new ConfirmResult(true, t))
                .ToList();
var meta = new Dictionary<string, object>
{
    { Msg.METADATA_CONFIRM_RESULTS, confirmResults }
};
Msg resumeMsg =
        Msg.Builder()
                .Name("user")
                .Role(MsgRole.USER)
                .TextContent("approved")
                .Metadata(meta)
                .Build();
await agent.CallAsync(new List<Msg> { resumeMsg });
```

If the resume is sent with `StreamEvents(new List<Msg> { resumeMsg })`, the stream includes a
`UserConfirmResultEvent` before the resumed tool execution. Use its `ReplyId` to associate
the accepted results with the earlier `RequireUserConfirmEvent`; the event contains only
the confirmations included in that resume call.

Comparison of the two modes:

| | Blocking `CallAsync()` | Streaming `StreamEvents()` |
|---|---|---|
| Getting pending tools | Filter `ToolUseBlock`s (state `ASKING`) from `Msg.GetContent()` | Get directly from `RequireUserConfirmEvent.GetToolCalls()` |
| Resuming | Same: build `ConfirmResult` in metadata and issue a new `CallAsync()` | Same |
| Use case | REST APIs, simple synchronous services | WebSocket, SSE, real-time UIs |

### Unattended mode

In CI or cron-job scenarios with no human operator, set the mode to `DONT_ASK` so that all ASK decisions degrade to DENY automatically:

```csharp
PermissionContextState headless =
        PermissionContextState.Builder()
                .Mode(PermissionMode.DONT_ASK)
                .AddAllowRule(
                        "safe_read",
                        new PermissionRule(
                                "safe_read", null, PermissionBehavior.ALLOW, "policy"))
                .Build();
// ASK-rule hits are auto-denied — no blocking wait
```

Full runnable example: `agentscope-examples/documentation/.../hitl/PermissionHITLExample.cs`.

## Common recipes

The examples below show how to configure `PermissionContext` for typical deployment scenarios. Each recipe combines a mode with a rule set tuned for one use case.

::::{tab-set}
:::{tab-item} Read-only exploration
```csharp
// EXPLORE mode: agent freely calls read-only tools; all writes are auto-denied.
PermissionContextState explore =
        PermissionContextState.Builder()
                .Mode(PermissionMode.EXPLORE)
                .Build();

ReActAgent explorer =
        ReActAgent.Builder()
                .Name("explorer")
                .SysPrompt("...")
                .Model(model)
                .PermissionContext(explore)
                .Build();
```
:::
:::{tab-item} Unattended automation
```csharp
using AgentScope.Core.Permission;

PermissionContextState ci =
        PermissionContextState.Builder()
                .Mode(PermissionMode.DONT_ASK)
                .AddAllowRule(
                        "deploy",
                        new PermissionRule(
                                "deploy", "staging", PermissionBehavior.ALLOW, "project"))
                .AddAllowRule(
                        "git_commit",
                        new PermissionRule(
                                "git_commit", null, PermissionBehavior.ALLOW, "project"))
                .Build();

ReActAgent ciAgent =
        ReActAgent.Builder()
                .Name("ci_agent")
                .SysPrompt("...")
                .Model(model)
                .PermissionContext(ci)
                .Build();
// Only explicitly allowed commands run; everything else is silently denied.
```
:::
:::{tab-item} Block dangerous commands
```csharp
PermissionContextState bypassWithDeny =
        PermissionContextState.Builder()
                .Mode(PermissionMode.BYPASS)
                .AddDenyRule(
                        "drop_table",
                        new PermissionRule(
                                "drop_table", null, PermissionBehavior.DENY, "userSettings"))
                .AddDenyRule(
                        "force_push",
                        new PermissionRule(
                                "force_push", null, PermissionBehavior.DENY, "userSettings"))
                .Build();
// Everything except the explicitly denied tools runs (deny rules can't be bypassed).
```
:::
::::
