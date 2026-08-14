namespace AgentScope.Extensions.Skill;

/// <summary>
/// 技能仓库接口。对标 Java AgentSkillRepository。
/// </summary>
public interface ISkillRepository
{
    Task<Skill?> GetSkillAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAllSkillNamesAsync(CancellationToken ct = default);
    Task<bool> SkillExistsAsync(string name, CancellationToken ct = default);
}

public sealed record Skill(string Name, string Description, string Content, string? Source = null);
