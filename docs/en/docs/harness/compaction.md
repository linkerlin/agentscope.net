---
title: "Context Compaction"
description: "CompactionMiddleware, ConversationCompactor, and tool result eviction"
---

## Overview

`AgentScope.Harness.Memory.Compaction` provides two levels of context management:

1. **Turn-level marking**: `CompactionMiddleware` checks context length each turn and marks whether compaction is needed;
2. **Message list compaction**: `ConversationCompactor` performs truncation / pruning / summarization on the message list according to `CompactionConfig`;
3. **Tool result eviction**: `ToolResultEvictionConfig` + `ToolResultEvictionMiddleware` offloads oversized tool results to disk, leaving only placeholders.

## CompactionMiddleware

```csharp
using AgentScope.Harness.Middleware;

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))   // default 4096
    .Build();
```

`HarnessAgentBuilder.Build()` also automatically assembles a default `CompactionMiddleware` (Order 700). It reads `ctx.Items["context_length"]` and sets `ctx.Items["needs_compaction"]` to `true` when the threshold is exceeded, for downstream components to consume.

## ConversationCompactor

Performs compaction judgment and compaction on any message list:

```csharp
using AgentScope.Harness.Memory.Compaction;

var compactor = new ConversationCompactor(new CompactionConfig
{
    TriggerMessageCount = 30,     // message count threshold (default 50)
    TriggerTokenCount = 8000,     // token count threshold (default 8000, estimated by len/4)
    TargetMessageCount = 15,      // max messages retained after compaction (default 20)
    TargetTokenCount = 3000,      // max tokens retained after compaction (default 3000)
    Mode = CompactionMode.Adaptive
});

if (compactor.ShouldCompact(messages))
{
    IReadOnlyList<Msg> compacted = compactor.Compact(messages);
}
```

### CompactionMode

| Mode | Behavior |
|------|------|
| `TruncateOnly` | Only truncate tool call parameters (`TruncateArgsConfig`: enabled by default, 500 chars per param, max 20 per call) |
| `PruneOnly` | Only prune tool results (`PruneConfig`: enabled by default, 1000 chars per result, max 10 per message, preserve error results) |
| `SummarizeOnly` | Only generate summaries (`SummarizeConfig`: **disabled** by default, configurable with `ModelName` / `MaxSummaryTokens=500` / `SummaryPrompt`) |
| `Adaptive` | (default) Applies the above three sub-strategies in sequence |

## Tool Result Eviction

`ToolResultEvictionConfig` defines the eviction strategy:

```csharp
using AgentScope.Harness.Memory.Compaction;

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithFilesystem(fs)
    .WithToolResultEviction(new ToolResultEvictionConfig
    {
        Enabled = true,
        MaxResultBytes = 4096,        // trigger threshold
        HeadBytes = 256,              // head bytes to retain
        TailBytes = 256,              // tail bytes to retain
        Placeholder = "... [Result truncated] ...",
        MaxResultChars = 4000,        // character threshold for eviction
        PreviewChars = 500,           // preview length retained in placeholder
        EvictionPath = ".evicted",    // directory for evicted content
        ExcludedToolNames = new HashSet<string> { "read_file", "readFile" }
    })
    .Build();
```

Setting `WithToolResultEviction(...)` automatically enables `ToolResultEvictionMiddleware` (Order 30): oversized tool results are written to the filesystem (deduplicated by content hash), leaving only `head + preview + tail` placeholders in the message, with metadata marking `agentscope.tool_result_evicted`. `config.Evict(result)` can also be called independently for in-memory truncation.

## Interaction with Memory Maintenance

`MemoryConsolidator` (see [Memory](./memory.md)) internally calls `ConversationCompactor` during consolidation; the compaction action itself is written to the session transcript log as a `CompactionEntry` (`Summary` / `OriginalMessageCount` / `CompressedMessageCount`), ensuring that already compacted content can be skipped during replay.

## Related Documentation

- [Memory](./memory.md) —— Transcript, flush, and consolidation
- [Context and AgentState](../building-blocks/context.md)
