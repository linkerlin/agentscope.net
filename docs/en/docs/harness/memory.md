---
title: "Memory"
description: "Session transcripts, memory flush, consolidation, and MEMORY.md"
---

## Overview

Harness's memory system (`AgentScope.Harness.Memory`) adds three disk-level facilities on top of Core's `IMemory`: **session transcripts** (raw logs, never compressed), **memory flush** (persisting key content to disk at turn end), and **memory consolidation** (periodically merging into long-term summaries).

Core-level `IMemory` / `SqliteMemory` / `InMemoryLongTermMemory` / `StateBackedMemory` are documented in [Context and AgentState](../building-blocks/context.md).

## MemoryConfig

```csharp
using AgentScope.Harness.Memory;

var config = new MemoryConfig
{
    ModelName = "qwen-plus",                       // model used for consolidation
    FlushPrompt = "Extract key facts, decisions, and preferences from the conversation:",   // flush prompt (default)
    ConsolidationPrompt = "Merge the following daily records into a long-term memory summary:",
    ConsolidationMaxTokens = 2048,
    ConsolidationMinGap = TimeSpan.FromHours(1),
    DailyFileRetentionDays = 30,                   // daily memory file retention period
    SessionRetentionDays = 7,                      // session log retention period
    FlushTrigger = FlushTriggerMode.Always         // Always / Never / Throttled
};
```

## Session Transcript

### SessionTranscriptWriter

Appends turn events to `{logDir}/{sessionId}.jsonl`:

```csharp
var writer = new SessionTranscriptWriter("transcripts", "demo-session");
await writer.WriteMessageAsync(msg);
await writer.WriteToolUseAsync("shell_command", toolCallId: "t1", arguments: "{\"command\":\"ls\"}");
await writer.WriteToolResultAsync("t1", result: "...", isError: false);
await writer.WriteCompactionAsync(summary: "...", originalCount: 40, compressedCount: 12);
List<SessionEntry> entries = await writer.ReadAllAsync();
```

### SessionEntry Types

JSON polymorphic entries (discriminated by `type`): `MessageEntry` / `ToolUseEntry` / `ToolResultEntry` / `CompactionEntry` (Summary, Original/CompressedMessageCount) / `SummaryEntry` (Summary, SourceMessageCount).

### SessionTree

Dual-file management (`{baseDir}/{sessionId}.ctx.jsonl` context + `.log.jsonl` log):

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

Evaluates session freshness: `IsIdleExpired(lastActivityTime)` (default idle 24h expiry), `ShouldResetDaily(lastActivityTime)` (daily reset), `GetNextResetTime()`.

## Memory Flush: MemoryFlushManager

```csharp
var flush = new MemoryFlushManager(config, writer);
await flush.FlushMessageAsync(msg);
await flush.FlushBatchAsync(messages);
await flush.FlushToolUseAsync("shell_command", "t1", arguments);
await flush.FlushToolResultAsync("t1", result, isError: false);
```

`MemoryFlushMiddleware` (Order 800, auto-assembled on Build) sets `memory_flush_pending` at turn end, consumed by the host to trigger the flush.

## Memory Consolidation: MemoryConsolidator

```csharp
var consolidator = new MemoryConsolidator(config, sessionTree);

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithWorkspaceRoot(".agentscope/workspace")
    .WithMemoryConsolidator(consolidator)     // automatically enables MemoryMaintenanceMiddleware
    .Build();
```

`ConsolidateAsync()` flow: read logs → filter message entries → compress via `ConversationCompactor` → write `CompactionEntry` markers → save context. `ShouldConsolidate(lastConsolidated)` checks whether the consolidation interval (`ConsolidationMinGap`) has been reached.

`MemoryMaintenanceMiddleware` (Order 900) runs at a minimum interval (default 30 minutes): archive expired daily memory files (default 90-day retention) → consolidate memory → clean up old session logs (default 180-day retention).

## Related Documentation

- [Context Compaction](./compaction.md) —— ConversationCompactor details
- [Workspace](./workspace.md) —— `MEMORY.md` injection and `memory/` directory
- [Long-term Memory Integration](../../integration/memory/index.md) —— Mem0 / ReMe / Bailian clients
