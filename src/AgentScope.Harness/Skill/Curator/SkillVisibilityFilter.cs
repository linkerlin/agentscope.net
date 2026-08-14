using AgentScope.Core.Agent;
using AgentScope.Core.Skill;

namespace AgentScope.Harness.Skill.Curator;

/// <summary>技能坯觝性过滤器，对�?Java SkillVisibilityFilter</summary>
public interface ISkillVisibilityFilter
{
    List<RegisteredSkill> Filter(List<RegisteredSkill> skills, RuntimeContext? context);
}

/// <summary>仅兝许白坝坕�?agent 创建�?skill</summary>
public sealed class AllowListFilter : ISkillVisibilityFilter
{
    private readonly HashSet<string> _allow;
    public AllowListFilter(IEnumerable<string> allow) => _allow = new(allow);

    public List<RegisteredSkill> Filter(List<RegisteredSkill> skills, RuntimeContext? context)
    {
        return skills.Where(s => _allow.Contains(s.Name)).ToList();
    }
}

/// <summary>按哈希百分比睰度放行</summary>
public sealed class CanaryFilter : ISkillVisibilityFilter
{
    private readonly int _percent;
    public CanaryFilter(int percent) => _percent = percent;

    public List<RegisteredSkill> Filter(List<RegisteredSkill> skills, RuntimeContext? context)
    {
        var userId = context?.UserId ?? "unknown";
        return skills.Where(s =>
            Math.Abs((userId + s.Name).GetHashCode()) % 100 < _percent).ToList();
    }
}

/// <summary>链弝组坈过滤器（AND�?/summary>
public sealed class CompositeFilter : ISkillVisibilityFilter
{
    private readonly List<ISkillVisibilityFilter> _filters;
    public CompositeFilter(IEnumerable<ISkillVisibilityFilter> filters) => _filters = new(filters);

    public List<RegisteredSkill> Filter(List<RegisteredSkill> skills, RuntimeContext? context)
    {
        var result = skills;
        foreach (var f in _filters)
            result = f.Filter(result, context);
        return result;
    }
}

/// <summary>按环境过�?/summary>
public sealed class EnvironmentFilter : ISkillVisibilityFilter
{
    private readonly string _environment;
    public EnvironmentFilter(string env) => _environment = env;

    public List<RegisteredSkill> Filter(List<RegisteredSkill> skills, RuntimeContext? context)
    {
        return skills.Where(s => s.SourcePath?.Contains(_environment) == true).ToList();
    }
}

