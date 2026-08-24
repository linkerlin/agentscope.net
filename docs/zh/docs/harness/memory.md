---
title: "记忆"
description: "会话转录、记忆刷写、整合与 MEMORY.md"
---

## 概述

Harness 的记忆体系（`AgentScope.Harness.Memory`）在 Core 的 `IMemory` 之上增加三层磁盘设施：**会话转录**（原始日志，永不压缩）、**记忆刷写**（回合结束把关键内容落盘）、**记忆整合**（定期合并为长期摘要）。

Core 层的 `IMemory` / `SqliteMemory` / `InMemoryLongTermMemory` / `StateBackedMemory` 见[上下文与 AgentState](../building-blocks/context.md)。

## MemoryConfig

```csharp
using AgentScope.Harness.Memory;

var config = new MemoryConfig
{
    ModelName = "qwen-plus",                       // 整合所用模型
    FlushPrompt = "提取对话中的关键事实、决策与偏好:",   // 刷写提示词（默认值）
    ConsolidationPrompt = "将以下每日记录合并为长期记忆摘要:",
    ConsolidationMaxTokens = 2048,
    ConsolidationMinGap = TimeSpan.FromHours(1),
    DailyFileRetentionDays = 30,                   // 每日记忆文件保留期
    SessionRetentionDays = 7,                      // 会话日志保留期
    FlushTrigger = FlushTriggerMode.Always         // Always / Never / Throttled
};
```

## 会话转录

### SessionTranscriptWriter

把回合事件追加写入 `{logDir}/{sessionId}.jsonl`：

```csharp
var writer = new SessionTranscriptWriter("transcripts", "demo-session");
await writer.WriteMessageAsync(msg);
await writer.WriteToolUseAsync("shell_command", toolCallId: "t1", arguments: "{\"command\":\"ls\"}");
await writer.WriteToolResultAsync("t1", result: "...", isError: false);
await writer.WriteCompactionAsync(summary: "...", originalCount: 40, compressedCount: 12);
List<SessionEntry> entries = await writer.ReadAllAsync();
```

### SessionEntry 类型

JSON 多态条目（`type` 鉴别器）：`MessageEntry` / `ToolUseEntry` / `ToolResultEntry` / `CompactionEntry`（Summary、Original/CompressedMessageCount）/ `SummaryEntry`（Summary、SourceMessageCount）。

### SessionTree

双文件管理（`{baseDir}/{sessionId}.ctx.jsonl` 上下文 + `.log.jsonl` 日志）：

```csharp
var tree = new SessionTree(".agentscope/sessions", "demo-session");
tree.Append(entry);
await tree.FlushAsync();
List<SessionEntry> ctx = await tree.LoadContextAsync();
List<SessionEntry> log = await tree.LoadLogAsync();
await tree.SaveContextAsync(newEntries);
long size = tree.GetLogSize();
```

### SessionFreshnessEvaluator

判断会话新鲜度：`IsIdleExpired(lastActivityTime)`（默认空闲 24 小时过期）、`ShouldResetDaily(lastActivityTime)`（每日重置）、`GetNextResetTime()`。

## 记忆刷写：MemoryFlushManager

```csharp
var flush = new MemoryFlushManager(config, writer);
await flush.FlushMessageAsync(msg);
await flush.FlushBatchAsync(messages);
await flush.FlushToolUseAsync("shell_command", "t1", arguments);
await flush.FlushToolResultAsync("t1", result, isError: false);
```

`MemoryFlushMiddleware`（Order 800，Build 时自动装配）在回合结束时标记 `memory_flush_pending`，由宿主消费该标记触发刷写。

## 记忆整合：MemoryConsolidator

```csharp
var consolidator = new MemoryConsolidator(config, sessionTree);

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithWorkspaceRoot(".agentscope/workspace")
    .WithMemoryConsolidator(consolidator)     // 自动启用 MemoryMaintenanceMiddleware
    .Build();
```

`ConsolidateAsync()` 流程：读取日志 → 过滤消息条目 → `ConversationCompactor` 压缩 → 写入 `CompactionEntry` 标记 → 保存上下文。`ShouldConsolidate(lastConsolidated)` 判断是否达到整合间隔（`ConsolidationMinGap`）。

`MemoryMaintenanceMiddleware`（Order 900）按最小间隔（默认 30 分钟）执行：归档过期日记忆文件（默认保留 90 天）→ 整合记忆 → 清理旧会话日志（默认保留 180 天）。

## 相关文档

- [上下文压缩](./compaction.md) —— ConversationCompactor 细节
- [工作区](./workspace.md) —— `MEMORY.md` 注入与 `memory/` 目录
- [长期记忆集成](../../integration/memory/index.md) —— Mem0 / ReMe / Bailian 客户端
