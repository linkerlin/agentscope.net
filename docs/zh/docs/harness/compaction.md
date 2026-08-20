---
title: "上下文压缩"
description: "CompactionMiddleware、ConversationCompactor 与工具结果驱逐"
---

## 概述

`AgentScope.Harness.Memory.Compaction` 提供两级上下文管理：

1. **回合级标记**：`CompactionMiddleware` 在每回合检查上下文长度并标记是否需要压缩；
2. **消息列表压缩**：`ConversationCompactor` 按 `CompactionConfig` 对消息列表执行截断 / 修剪 / 摘要；
3. **工具结果驱逐**：`ToolResultEvictionConfig` + `ToolResultEvictionMiddleware` 把超大工具结果落盘，只留占位符。

## CompactionMiddleware

```csharp
using AgentScope.Harness.Middleware;

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))   // 默认 4096
    .Build();
```

`HarnessAgentBuilder.Build()` 也会自动装配一个默认参数的 `CompactionMiddleware`（Order 700）。它读取 `ctx.Items["context_length"]`，超出阈值时把 `ctx.Items["needs_compaction"]` 置为 `true`，供下游组件消费。

## ConversationCompactor

对任意消息列表执行压缩判断与压缩：

```csharp
using AgentScope.Harness.Memory.Compaction;

var compactor = new ConversationCompactor(new CompactionConfig
{
    TriggerMessageCount = 30,     // 消息数阈值（默认 50）
    TriggerTokenCount = 8000,     // token 数阈值（默认 8000，按 len/4 估算）
    TargetMessageCount = 15,      // 压缩后保留的最大消息数（默认 20）
    TargetTokenCount = 3000,      // 压缩后保留的最大 token 数（默认 3000）
    Mode = CompactionMode.Adaptive
});

if (compactor.ShouldCompact(messages))
{
    IReadOnlyList<Msg> compacted = compactor.Compact(messages);
}
```

### CompactionMode

| 模式 | 行为 |
|------|------|
| `TruncateOnly` | 只截断工具调用参数（`TruncateArgsConfig`：默认开启，单参数 500 字符、每调用最多 20 个） |
| `PruneOnly` | 只修剪工具结果（`PruneConfig`：默认开启，单结果 1000 字符、每消息最多 10 个、保留错误结果） |
| `SummarizeOnly` | 只生成摘要（`SummarizeConfig`：默认**关闭**，可配 `ModelName` / `MaxSummaryTokens=500` / `SummaryPrompt`） |
| `Adaptive` |（默认）依次应用以上三种子策略 |

## 工具结果驱逐

`ToolResultEvictionConfig` 定义驱逐策略：

```csharp
using AgentScope.Harness.Memory.Compaction;

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithFilesystem(fs)
    .WithToolResultEviction(new ToolResultEvictionConfig
    {
        Enabled = true,
        MaxResultBytes = 4096,        // 触发阈值
        HeadBytes = 256,              // 保留头部
        TailBytes = 256,              // 保留尾部
        Placeholder = "... [结果已截断] ...",
        MaxResultChars = 4000,        // 落盘驱逐的字符阈值
        PreviewChars = 500,           // 占位符中保留的预览长度
        EvictionPath = ".evicted",    // 驱逐内容落盘目录
        ExcludedToolNames = new HashSet<string> { "read_file", "readFile" }
    })
    .Build();
```

设置 `WithToolResultEviction(...)` 后自动启用 `ToolResultEvictionMiddleware`（Order 30）：超阈值工具结果写入文件系统（按内容哈希去重），消息里只留 `头 + 预览 + 尾` 占位符，并在元数据中标记 `agentscope.tool_result_evicted`。`config.Evict(result)` 也可单独调用执行内存截断。

## 与记忆维护的联动

`MemoryConsolidator`（见[记忆](./memory.md)）在整合时内部调用 `ConversationCompactor`；压缩动作本身会以 `CompactionEntry`（`Summary` / `OriginalMessageCount` / `CompressedMessageCount`）写入会话转录日志，保证回放时可跳过已压缩内容。

## 相关文档

- [记忆](./memory.md) —— 转录、刷写与整合
- [上下文与 AgentState](../building-blocks/context.md)
