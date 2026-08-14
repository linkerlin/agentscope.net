namespace AgentScope.Harness.Skill.Curator;

/// <summary>SkillCurator 配置，对应 Java SkillCuratorConfig</summary>
public sealed record SkillCuratorConfig
{
    public UmbrellaPassMode UmbrellaMode { get; init; } = UmbrellaPassMode.Disabled;
    public TimeSpan RunInterval { get; init; } = TimeSpan.FromHours(6);
    public TimeSpan DraftTimeout { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan StaleTimeout { get; init; } = TimeSpan.FromDays(30);

    public enum UmbrellaPassMode
    {
        Disabled,
        DryRunOnly
    }
}
