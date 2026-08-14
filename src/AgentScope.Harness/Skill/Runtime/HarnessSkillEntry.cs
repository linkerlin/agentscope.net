using AgentScope.Core.Skill;

namespace AgentScope.Harness.Skill.Runtime;

/// <summary>AgentSkill 的调用包装，附带懒加载资源，对应 Java HarnessSkillEntry</summary>
public sealed record HarnessSkillEntry(
    RegisteredSkill Skill,
    SkillResources? Resources = null,
    string? FilesRoot = null)
{
    public string SkillId => Skill.Name;
}

/// <summary>技能资源，对应 Java SkillResources</summary>
public sealed record SkillResources(
    Dictionary<string, string>? Files = null,
    string? Content = null);
