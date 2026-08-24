---
title: "Subagent"
description: "SubagentDeclaration, DefaultAgentManager, and dynamic creation"
---

## Overview

Subagents (`AgentScope.Harness.Subagent`) allow the main Agent to delegate tasks to independently created agents. The system is separated into three layers: declaration, factory, and manager.

### SubagentDeclaration

```csharp
public sealed record SubagentDeclaration(
    string Name,
    string Description,
    string? WorkspacePath = null,     // associated workspace path
    string? InlineBody = null,        // inline Markdown spec body
    string? RemoteUrl = null,         // remote Agent URL (A2A/Agent Protocol)
    WorkspaceMode WorkspaceMode = WorkspaceMode.Shared)   // Shared (default) / Isolated
{
    public bool IsRemote => RemoteUrl != null;
}
```

### From Markdown Declaration

`AgentSpecLoader` parses YAML front matter + Markdown body:

```
workspace/subagents/researcher.md
---
name: researcher
description: Search for information and summarize key points
---
You are a rigorous researcher. After receiving a topic, first search, then output a structured summary.
```

```csharp
SubagentDeclaration decl = AgentSpecLoader.Load("researcher");          // load by name from subagents/ directory
SubagentDeclaration parsed = AgentSpecLoader.Parse(markdown, "name");   // parse string directly
```

### Factory

```csharp
public delegate IAgent SubagentFactory(SubagentDeclaration declaration);
```

Typical implementation: build an `EnhancedReActAgent` with a dedicated system prompt (the declaration body).

### Manager

```csharp
public interface ISubagentManager
{
    IAgent GetOrCreate(string specRef);   // creates via factory if missing (specRef is usually the declaration name)
    void Register(string name, IAgent agent);
    void Remove(string name);
}

// Default implementation: thread-safe registry
var manager = new DefaultAgentManager(factory);
```

## Integration with Agent

```csharp
using AgentScope.Harness;
using AgentScope.Harness.Subagent;

SubagentDeclaration declaration = AgentSpecLoader.Load("researcher");
ISubagentManager manager = new DefaultAgentManager(decl =>
{
    var sub = new EnhancedReActAgentBuilder()
        .Name(decl.Name)
        .SysPrompt(decl.InlineBody ?? decl.Description)
        .Model(subModel)
        .Build();
    return sub;
});

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(mainModel)
    .WithSubagentManager(manager)     // default DefaultAgentManager() (factory is null)
    .Build();
```

`SubagentsMiddleware` (Order 300, auto-assembled) injects the manager into `ctx.Items["subagents"]`, consumed by tools like `AgentSpawnTool` — the model triggers `GetOrCreate` and calls the subagent via `agent_spawn`-like tools.

## Remote Subagent

When a declaration carries `RemoteUrl`, it is a remote subagent. Calls are forwarded through the following protocol facilities:

- `RemoteSubagentStub` / `RemoteAskPolicy`: local stub and ask policy;
- `SubagentGatewayBridge`: bridge with Gateway;
- `Tasks/` sub-package: `BackgroundTask` / `TaskRepository` / `WorkspaceTaskRepository` / `AgentProtocolTaskClient` and other background task facilities (submission, status polling, result callback).

## Team

Multi-agent task-level collaboration uses `ITeamClient` (`AgentScope.Harness.Team`):

```csharp
ITeamClient teams = new LocalTeamClient();   // in-process implementation, CAS optimistic concurrency

string taskId = await teams.CreateTaskAsync(new TeamTask(Id: "", Description: "Research competitors"));
bool claimed = await teams.ClaimTaskAsync(taskId, memberId: "agent-1");
await teams.CompleteTaskAsync(taskId, result: "...");
await foreach (TeamMessage msg in teams.ReadMessagesAsync(inbox: "agent-1")) { }
await teams.SendMessageAsync("agent-2", new TeamMessage("agent-1", "agent-2", "hi", DateTime.UtcNow));
```

`TeamsMiddleware` (Order 500, auto-assembled) injects `ctx.Items["team"]`; `TeamTool` exposes task/message operations as model tools. `TeamCreateSpec(Name, Description?, MemberIds?)` is used for creating teams.

## Related Documentation

- [Harness Architecture](./architecture.md)
- [Channel](./channel.md) —— Cross-process message entry point
