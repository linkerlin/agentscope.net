namespace AgentScope.Harness.Memory;

/// <summary>
/// 内存管理配置。对标 Java MemoryConfig。
/// </summary>
public sealed record MemoryConfig
{
    public string? ModelName { get; init; }
    public string FlushPrompt { get; init; } = "提取对话中的关键事实、决策与偏好:";
    public string ConsolidationPrompt { get; init; } = "将以下每日记录合并为长期记忆摘要:";
    public int ConsolidationMaxTokens { get; init; } = 2048;
    public TimeSpan? ConsolidationMinGap { get; init; } = TimeSpan.FromHours(1);
    public int DailyFileRetentionDays { get; init; } = 30;
    public int SessionRetentionDays { get; init; } = 7;
    public FlushTriggerMode FlushTrigger { get; init; } = FlushTriggerMode.Always;

    public enum FlushTriggerMode { Always, Never, Throttled }
}
