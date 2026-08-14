using AgentScope.Core.Message;

namespace AgentScope.Harness.Memory.Compaction;

/// <summary>对话压缩器，使用组合策略缩减对话历史</summary>
public sealed class ConversationCompactor
{
    private readonly CompactionConfig _config;
    private readonly ToolResultEvictionConfig _eviction;

    public ConversationCompactor(
        CompactionConfig? config = null,
        ToolResultEvictionConfig? eviction = null)
    {
        _config = config ?? new CompactionConfig();
        _eviction = eviction ?? new ToolResultEvictionConfig();
    }

    public CompactionConfig Config => _config;

    /// <summary>判断是否需要压缩</summary>
    public bool ShouldCompact(IReadOnlyList<Msg> messages)
    {
        if (messages.Count >= _config.TriggerMessageCount) return true;
        var totalTokens = EstimateTokens(messages);
        return totalTokens >= _config.TriggerTokenCount;
    }

    /// <summary>执行压缩，返回压缩后的消息列表</summary>
    public IReadOnlyList<Msg> Compact(IReadOnlyList<Msg> messages)
    {
        if (!ShouldCompact(messages)) return messages;

        var working = messages.ToList();

        if (_config.TruncateArgs?.Enabled == true)
            working = TruncateToolArgs(working);

        if (_config.Prune?.Enabled == true)
            working = PruneToolResults(working);

        // 如果仍然超出目标，丢弃最早的非系统消息
        while (working.Count > _config.TargetMessageCount)
        {
            var idx = FindEarliestNonSystem(working);
            if (idx < 0) break;
            working.RemoveAt(idx);
        }

        return working;
    }

    private List<Msg> TruncateToolArgs(List<Msg> messages)
    {
        // C# Msg 使用单一 Content 字段，工具调用参数在 Content 中作为字符串传递
        return messages;
    }

    private List<Msg> PruneToolResults(List<Msg> messages)
    {
        // ToolResultBlock 在 C# 中是标记 record，实际结果内容在 Msg.Content 中
        return messages;
    }

    private static int FindEarliestNonSystem(IList<Msg> messages)
    {
        for (var i = 0; i < messages.Count; i++)
            if (messages[i].Role != "system") return i;
        return -1;
    }

    private int EstimateTokens(IReadOnlyList<Msg> messages)
    {
        return messages.Sum(m =>
            TokenCounterUtil.EstimateTokenCount(m.GetTextContent()));
    }
}
