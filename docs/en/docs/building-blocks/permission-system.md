---
title: "Permission System"
description: "IPermissionEngine / PermissionEngine rules and HITL confirmation"
---

## Overview

The permission system (`AgentScope.Core.Permission`) makes three-state decisions **before each tool execution**:

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

The built-in implementation `PermissionEngine` uses a 6-step priority state machine (deny > ask > built-in allow > allow > bypass > default ask):

```csharp
var permission = new PermissionEngine(PermissionMode.Default)
    .AddRule("calculator", PermissionBehavior.Allow)          // Exact name / wildcard pattern
    .AddRule("shell_command*", PermissionBehavior.Ask)        // Shell series needs confirmation
    .AddRule("write_file", PermissionBehavior.Deny);          // Deny file writing

PermissionDecision decision = permission.Evaluate(
    new ToolCallRequest { ToolName = "shell_command", Arguments = new() { ["command"] = "ls" } });
// decision.Behavior == PermissionBehavior.Ask
```

Notes:

- `CalculatorTool` (`calculator`) and `GetTimeTool` (`get_time`) are **auto-allowed** at step 3 of the state machine;
- In `PermissionMode.Bypass` mode, calls that do not match deny/ask rules are allowed directly;
- `AddRule` returns itself, supporting chained calls.

## Integration with Agent

Inject via `EnhancedReActAgentBuilder.PermissionEngine(...)`; on the `HarnessAgentBuilder` side, use `WithPermission(...)`:

```csharp
EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(model)
    .PermissionEngine(new PermissionEngine()
        .AddRule("shell_command*", PermissionBehavior.Ask))
    .ConfirmCallback(async confirmEvent =>
    {
        Console.WriteLine($"Tool {confirmEvent.ToolName} requests execution");
        return Console.ReadLine() == "y"
            ? ConfirmResult.Approve()
            : ConfirmResult.Deny("User denied");
    })
    .Build();
```

Decision behavior:

| Decision | Behavior |
|----------|----------|
| `Allow` | Execute directly |
| `Deny` | Returns `ActionResult.ToolCall(..., success: false, "Permission denied: ...")`; the model sees the error result |
| `Ask` | Triggers HITL: calls `ConfirmCallback` if set; otherwise prompts `y/N` on console; when no interactive terminal, follows `AutoApproveOnAsk(true)` to allow or deny |

## Permission State Persistence

The `PermissionContextState` record (`Mode`, `WorkingDirectory`, `AllowRules`, `DenyRules`, `AskRules`) is a snapshot of the permission engine state that can be serialized with `Session` for cross-process permission rule restoration. Also includes the `AdditionalWorkingDirectory(Path, Source)` record for expressing additional writable directories.

## Related Documentation

- [Agent — Human-in-the-Loop](./agent.md#human-in-the-loophitl)
- [Tool](./tool.md) — Tool definition and execution
