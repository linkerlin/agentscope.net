namespace AgentScope.Harness.Memory.Compaction;

/// <summary>对话压缩配置，控制何时以及如何压缩对话历史</summary>
public sealed record CompactionConfig
{
    /// <summary>触发压缩的消息数阈值</summary>
    public int TriggerMessageCount { get; init; } = 50;

    /// <summary>触发压缩的 token 数阈值</summary>
    public int TriggerTokenCount { get; init; } = 8000;

    /// <summary>压缩后保留的最大消息数</summary>
    public int TargetMessageCount { get; init; } = 20;

    /// <summary>压缩后保留的最大 token 数</summary>
    public int TargetTokenCount { get; init; } = 3000;

    /// <summary>压缩模式</summary>
    public CompactionMode Mode { get; init; } = CompactionMode.Adaptive;

    /// <summary>参数截断配置</summary>
    public TruncateArgsConfig? TruncateArgs { get; init; }

    /// <summary>工具结果剪枝配置</summary>
    public PruneConfig? Prune { get; init; }

    /// <summary>LLM 汇总配置</summary>
    public SummarizeConfig? Summarize { get; init; }
}

public enum CompactionMode
{
    /// <summary>仅截断参数</summary>
    TruncateOnly,
    /// <summary>仅剪枝工具结果</summary>
    PruneOnly,
    /// <summary>仅 LLM 汇总</summary>
    SummarizeOnly,
    /// <summary>自适应组合策略</summary>
    Adaptive
}

/// <summary>参数截断配置：截断过长的工具调用参数</summary>
public sealed record TruncateArgsConfig
{
    public bool Enabled { get; init; } = true;
    public int MaxArgLength { get; init; } = 500;
    public int MaxArgsPerCall { get; init; } = 20;
}

/// <summary>工具结果剪枝配置：移除或缩减大型工具输出</summary>
public sealed record PruneConfig
{
    public bool Enabled { get; init; } = true;
    public int MaxResultLength { get; init; } = 1000;
    public int MaxResultsPerMessage { get; init; } = 10;
    public bool KeepErrorResults { get; init; } = true;
}

/// <summary>LLM 汇总配置：使用 LLM 生成对话摘要代替原始消息</summary>
public sealed record SummarizeConfig
{
    public bool Enabled { get; init; } = false;
    public string? ModelName { get; init; }
    public int MaxSummaryTokens { get; init; } = 500;
    public string SummaryPrompt { get; init; } =
        "请将以下对话压缩为简洁的摘要，保留关键信息、决策和工具调用结果。";
}
