---
title: "Middleware"
description: "Core MiddlewareBase / Hook 与 Harness IHarnessMiddleware 管道"
---

## 概述

AgentScope .NET 有两套互补的拦截机制：

| 机制 | 所在包 | 挂点 | 适用 |
|------|--------|------|------|
| **Hook**（`IHook` + `HookManager`） | `AgentScope.Core` | 推理 / 行动 / 摘要前后及流式块 | 观察与终止 ReAct 循环 |
| **IHarnessMiddleware** | `AgentScope.Harness` | 回合（OnAgent）/ 模型调用 / 工具执行 / 系统提示词 | 包装整个 Agent 回合（洋葱模型） |

Hook 的用法见[智能体 — Hook 体系](./agent.md#hook-体系)。本文聚焦 `IHarnessMiddleware`。

## IHarnessMiddleware

```csharp
public interface IHarnessMiddleware
{
    int Order { get; }   // 越小越先执行

    ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default);
    ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default);
    ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default);

    // 可选：改写系统提示词（默认原样返回）
    ValueTask<string> OnSystemPromptAsync(MiddlewareContext ctx, string prompt, CancellationToken ct = default);
}
```

### MiddlewareContext

| 属性 | 类型 | 说明 |
|------|------|------|
| `AgentName` | `string` | 被调用的 Agent 名称 |
| `Model` | `string?` | 模型标识（可选） |
| `ToolName` | `string?` | 工具名称（可选） |
| `Messages` | `List<Msg>` | 当前回合消息列表（可写） |
| `ToolCalls` | `List<ToolUseBlock>` | 本轮待执行的工具调用 |
| `Runtime` | `RuntimeContext?` | 运行时上下文 |
| `UserId` / `SessionId` | `string` | 计算属性，来自 `Runtime` |
| `Items` | `Dictionary<string, object?>` | 中间件间共享的键值存储（如 `filesystem`、`bus`、`session_id`、`plan_mode`、`needs_compaction`） |

### 执行模型（洋葱模型）

`HarnessAgent.CallAsync` 在调用内层 `EnhancedReActAgent` 前：

1. 按 `Order` 升序排序全部中间件；
2. 依次调用 `OnSystemPromptAsync` 改写系统提示词并写回内层 Agent；
3. 以洋葱模型执行 `OnAgentAsync` 链——每个中间件可以在 `next()` 前后做事，也可以不调用 `next()` 短路整个回合（短路时框架回退为直接执行核心调用，保持调用语义）。

## 自定义中间件

```csharp
using AgentScope.Harness.Middleware;

public sealed class AuditMiddleware : IHarnessMiddleware
{
    public int Order => 50;   // 见下方内置 Order 表

    public async ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await next();                          // 调用后续链
        sw.Stop();
        Console.WriteLine($"[{ctx.AgentName}] 回合耗时 {sw.ElapsedMilliseconds}ms，会话 {ctx.SessionId}");
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
    {
        Console.WriteLine($"[tool] {ctx.ToolName}");
        return next();
    }
}

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithMiddleware(new AuditMiddleware())     // 可多次调用，按 Order 排序
    .Build();
```

## 内置中间件一览

`HarnessAgentBuilder.Build()` 自动装配以下中间件（无需手动添加），自定义中间件通过 `WithMiddleware` 追加后与它们一起按 `Order` 排序：

| Order | 中间件 | 功能 |
|-------|--------|------|
| 20 | `AtPathExpansionMiddleware(WorkspaceManager)` | 展开 `@path` 引用为 `<attached_file>` 标签（最多 1000 行） |
| 25 | `WorkspaceContextMiddleware(WorkspaceManager, agentName, ...)` | 注入工作区上下文 / 域知识 / 记忆到系统提示词（token 预算 8000） |
| 30 | `ToolResultEvictionMiddleware(IFilesystem, ToolResultEvictionConfig?)` | 超大工具结果落盘并替换为占位符 |
| 50 | `SandboxLifecycleMiddleware(SandboxManager?)` | 注入沙箱上下文 |
| 100 | `AgentTraceMiddleware` | 记录回合开始 / 结束及耗时 |
| 200 | `InboxMiddleware(IMessageBus)` | 回合开始前 drain 收件箱消息 |
| 300 | `SubagentsMiddleware(ISubagentManager)` | 注入子 Agent 管理器 |
| 400 | `PlanModeMiddleware` | Plan 模式下追加规划指令到系统提示词 |
| 500 | `TeamsMiddleware(ITeamClient)` | 注入团队客户端 |
| 700 | `CompactionMiddleware(int maxContextLength = 4096)` | 上下文超长时标记 `needs_compaction` |
| 760 | `SkillUsageMiddleware(SkillUsageStore)` | 统计技能查看 / 使用次数 |
| 780 | `SkillCuratorMiddleware(SkillCurator)` | 回合结束后台触发技能策展 |
| 800 | `MemoryFlushMiddleware` | 回合结束标记待刷写记忆 |
| 900 | `TranscriptMiddleware(ITranscriptStore)` | 记录回合转录 |
| 900 | `MemoryMaintenanceMiddleware(WorkspaceManager, MemoryConsolidator?, ...)` | 定期归档日志、整合记忆 |

其中 `CompactionMiddleware`、`TranscriptMiddleware` 等在每次 Build 都会装配；工作区相关三件套（`WorkspaceContext` / `AtPathExpansion` / `MemoryMaintenance`）仅在 `WithWorkspace(...)` 配置后启用；`ToolResultEviction`、`SkillUsage`、`SkillCurator` 需显式配置对应组件。

## 相关文档

- [智能体](./agent.md) —— Core 层 Hook 体系
- [Harness 架构](../harness/architecture.md) —— 中间件在 HarnessAgent 中的装配
