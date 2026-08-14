using AgentScope.Core.Message;
using AgentScope.Harness.Memory.Compaction;
using AgentScope.Harness.Memory.Session;

namespace AgentScope.Harness.Memory;

/// <summary>记忆合并器：将日记录账合并为长期记忆摘要</summary>
public sealed class MemoryConsolidator
{
    private readonly MemoryConfig _config;
    private readonly SessionTree _sessionTree;
    private readonly ConversationCompactor _compactor;

    public MemoryConsolidator(
        MemoryConfig config,
        SessionTree sessionTree,
        ConversationCompactor? compactor = null)
    {
        _config = config;
        _sessionTree = sessionTree;
        _compactor = compactor ?? new ConversationCompactor();
    }

    /// <summary>执行合并：读取日志 → 压缩 → 保存上下文</summary>
    public async Task ConsolidateAsync(CancellationToken ct = default)
    {
        var logEntries = await _sessionTree.LoadLogAsync(ct);
        if (logEntries.Count == 0) return;

        // 提取消息条目
        var messages = logEntries
            .OfType<MessageEntry>()
            .Select(e => e.ToMsg())
            .ToList();

        if (messages.Count == 0) return;

        // 压缩
        var compressed = _compactor.ShouldCompact(messages)
            ? _compactor.Compact(messages)
            : messages;

        // 转换为上下文条目
        var contextEntries = compressed
            .Select(m => (SessionEntry)MessageEntry.FromMsg(m))
            .ToList();

        // 写入压缩标记
        contextEntries.Insert(0, new CompactionEntry
        {
            Summary = $"Consolidated {logEntries.Count} entries to {contextEntries.Count}",
            OriginalMessageCount = logEntries.Count,
            CompressedMessageCount = contextEntries.Count
        });

        await _sessionTree.SaveContextAsync(contextEntries, ct);
    }

    /// <summary>判断是否需要合并（基于时间间隔）</summary>
    public bool ShouldConsolidate(DateTime lastConsolidated)
    {
        var gap = _config.ConsolidationMinGap ?? TimeSpan.FromHours(1);
        return DateTime.UtcNow - lastConsolidated > gap;
    }
}
