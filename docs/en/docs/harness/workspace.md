---
title: "Workspace"
description: "WorkspaceManager and .agentscope directory layout"
---

## Overview

The workspace (`AgentScope.Harness.Workspace`) stores the agent's personality, knowledge, memory, skills, and subagent declarations on disk, injecting context on demand.

## Directory Layout

`WorkspaceConstants` defines the default layout (default root `.agentscope/workspace`):

```
.agentscope/workspace/
├── AGENTS.md          ← Agent personality description (injected into system prompt)
├── MEMORY.md          ← Long-term memory summary (injected into system prompt)
├── KNOWLEDGE.md       ← Domain knowledge (injected into system prompt)
├── knowledge/         ← Domain knowledge file directory
├── memory/            ← Daily memory stream (memory/archive/ for archived files)
├── skills/            ← Markdown skill repository
├── subagents/         ← Subagent declarations (.md)
├── agents/<agentName>/sessions/   ← Session transcript logs
├── rules/  tasks/  .index/        ← Other conventional directories
└── tools.json         ← Tool configuration (MCP servers, etc.)
```

## WorkspaceManager

```csharp
using AgentScope.Harness.Workspace;

var ws = new WorkspaceManager(".agentscope/workspace", sandboxed: true);   // IAsyncDisposable

// Read / Write (relative to root; sandboxed mode anchors to root, rejects .. traversal)
string? content = await ws.ReadAsync("AGENTS.md");
await ws.WriteAsync("notes/todo.md", "- Finish documentation\n");

// Built-in convention files
string? agentsMd  = await ws.ReadAgentsMdAsync();
string? memoryMd  = await ws.ReadMemoryMdAsync();
string? knowledgeMd = await ws.ReadKnowledgeMdAsync();

// Query
bool exists = ws.Exists("notes/todo.md");
var files = ws.ListFiles("notes", pattern: "*.md");
var knowledge = ws.ListKnowledgeFiles();
DateTime? lastWrite = ws.GetLastWriteTimeUtc("AGENTS.md");

// Management
ws.Move("notes/todo.md", "notes/done.md");
ws.Delete("notes/done.md");
await ws.DisposeAsync();
```

Reads go through a two-layer path of "memory cache → disk"; writes update the cache simultaneously.

## PathPolicy and LocalFsMode

- `PathPolicy(allowedRoots, denied?)`: `EnsureAllowed(path)` validates paths; `PathPolicy.FromWorkspace(root)` generates a policy that only allows the workspace root.
- `LocalFsMode` (filesystem isolation level, see [Filesystem](./filesystem.md)): `Sandboxed` (default, anchors to root) / `Rooted` (whitelisted roots) / `Unrestricted` (pass through as-is).

## Integration with Agent

```csharp
HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))   // or WithWorkspace(ws)
    .Build();
```

Configuring a workspace automatically enables three middlewares:

| Middleware | Order | Description |
|--------|-------|------|
| `WorkspaceContextMiddleware` | 25 | Injects `AGENTS.md` / `KNOWLEDGE.md` / `MEMORY.md` content into the system prompt within a token budget (default 8000) |
| `AtPathExpansionMiddleware` | 20 | Expands `@relative-path` in user messages into `<attached_file>` blocks (max 1000 lines) |
| `MemoryMaintenanceMiddleware` | 900 | Archives expired logs and performs memory consolidation at a minimum interval (default 30 minutes) (requires `WithMemoryConsolidator`) |

## tools.json

The `tools.json` file in the workspace root describes tool configurations such as MCP servers (loaded via `ToolsConfigLoader.LoadAsync(path)`):

```json
{
  "McpServers": [
    {
      "Name": "fs-server",
      "Command": "node",
      "Args": ["mcp-server.js"],
      "Env": { }
    }
  ]
}
```

Corresponding records: `ToolsConfig(List<McpServerConfig> McpServers, ToolFilter? Filter)`, `McpServerConfig(string Name, string Command, string[]? Args = null, Dictionary<string, string>? Env = null)`. `ToolFilter(AllowedNames?, DeniedNames?)`'s `IsAllowed(ITool)` controls tool visibility.

## Related Documentation

- [Filesystem](./filesystem.md) —— IFilesystem abstraction and three deployment modes
- [Skill](./skill.md) —— Markdown skills in the `skills/` directory
- [Memory](./memory.md) —— `memory/` and session transcripts
