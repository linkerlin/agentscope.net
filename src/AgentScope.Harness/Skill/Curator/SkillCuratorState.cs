namespace AgentScope.Harness.Skill.Curator;

/// <summary>Curator 持久化状态，对应 Java SkillCuratorState</summary>
public sealed record SkillCuratorState
{
    public DateTime? LastRunAt { get; init; }
    public int RunCount { get; init; }
    public bool Paused { get; init; }
    public string? LastRunSummary { get; init; }
    public int LastRunDurationSeconds { get; init; }
}
