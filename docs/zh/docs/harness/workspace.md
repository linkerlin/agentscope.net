---
title: "工作区"
description: "WorkspaceManager 与 .agentscope 目录布局"
---

## 概述

工作区（`AgentScope.Harness.Workspace`）把智能体的人格、知识、记忆、技能、子 Agent 声明全部落在磁盘上，按需注入上下文。

## 目录布局

`WorkspaceConstants` 定义默认布局（根目录默认 `.agentscope/workspace`）：

```
.agentscope/workspace/
├── AGENTS.md          ← 智能体人格说明（注入系统提示词）
├── MEMORY.md          ← 长期记忆摘要（注入系统提示词）
├── KNOWLEDGE.md       ← 域知识（注入系统提示词）
├── knowledge/         ← 域知识文件目录
├── memory/            ← 每日记忆流水（memory/archive/ 存放归档）
├── skills/            ← Markdown 技能仓库
├── subagents/         ← 子 Agent 声明（.md）
├── agents/<agentName>/sessions/   ← 会话转录日志
├── rules/  tasks/  .index/        ← 其他约定目录
└── tools.json         ← 工具配置（MCP 服务器等）
```

## WorkspaceManager

```csharp
using AgentScope.Harness.Workspace;

var ws = new WorkspaceManager(".agentscope/workspace", sandboxed: true);   // IAsyncDisposable

// 读 / 写（相对根目录；sandboxed 模式下锚定根目录，拒绝 .. 遍历）
string? content = await ws.ReadAsync("AGENTS.md");
await ws.WriteAsync("notes/todo.md", "- 完成文档\n");

// 内置约定文件
string? agentsMd  = await ws.ReadAgentsMdAsync();
string? memoryMd  = await ws.ReadMemoryMdAsync();
string? knowledgeMd = await ws.ReadKnowledgeMdAsync();

// 查询
bool exists = ws.Exists("notes/todo.md");
var files = ws.ListFiles("notes", pattern: "*.md");
var knowledge = ws.ListKnowledgeFiles();
DateTime? lastWrite = ws.GetLastWriteTimeUtc("AGENTS.md");

// 管理
ws.Move("notes/todo.md", "notes/done.md");
ws.Delete("notes/done.md");
await ws.DisposeAsync();
```

读取走「内存缓存 → 磁盘」双层；写入同时更新缓存。

## PathPolicy 与 LocalFsMode

- `PathPolicy(allowedRoots, denied?)`：`EnsureAllowed(path)` 校验路径；`PathPolicy.FromWorkspace(root)` 生成仅允许工作区根的策略。
- `LocalFsMode`（文件系统隔离级别，见[文件系统](./filesystem.md)）：`Sandboxed`（默认，锚定根目录）/ `Rooted`（白名单根目录）/ `Unrestricted`（原样通过）。

## 与 Agent 集成

```csharp
HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))   // 或 WithWorkspace(ws)
    .Build();
```

配置工作区后自动启用三个中间件：

| 中间件 | Order | 作用 |
|--------|-------|------|
| `WorkspaceContextMiddleware` | 25 | 把 `AGENTS.md` / `KNOWLEDGE.md` / `MEMORY.md` 内容按 token 预算（默认 8000）注入系统提示词 |
| `AtPathExpansionMiddleware` | 20 | 把用户消息中的 `@相对路径` 展开为 `<attached_file>` 块（最多 1000 行） |
| `MemoryMaintenanceMiddleware` | 900 | 按最小间隔（默认 30 分钟）归档过期日志、执行记忆整合（需配 `WithMemoryConsolidator`） |

## tools.json

工作区根目录的 `tools.json` 描述 MCP 服务器等工具配置（`ToolsConfigLoader.LoadAsync(path)` 加载）：

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

对应 record：`ToolsConfig(List<McpServerConfig> McpServers, ToolFilter? Filter)`、`McpServerConfig(string Name, string Command, string[]? Args = null, Dictionary<string, string>? Env = null)`。`ToolFilter(AllowedNames?, DeniedNames?)` 的 `IsAllowed(ITool)` 控制工具可见性。

## 相关文档

- [文件系统](./filesystem.md) —— IFilesystem 抽象与三种部署模式
- [技能](./skill.md) —— `skills/` 目录的 Markdown 技能
- [记忆](./memory.md) —— `memory/` 与会话转录
