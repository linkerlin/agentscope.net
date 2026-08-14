namespace AgentScope.Harness.Skill.Curator;

/// <summary>Skill 使用记录遥测，对应 Java SkillUsageRecord</summary>
public sealed record SkillUsageRecord
{
    public string SkillId { get; init; } = "";
    public SkillState State { get; init; } = SkillState.Active;
    public int ViewCount { get; init; }
    public int UseCount { get; init; }
    public int PatchCount { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; init; }
    public bool IsAgentCreated { get; init; }
    public string? CreatedBySessionId { get; init; }

    public DateTime LatestActivityAt => LastUsedAt ?? CreatedAt;
    public int ActivityCount => ViewCount + UseCount + PatchCount;
}

public enum SkillState
{
    Draft,
    Active,
    Stale,
    Archived
}
