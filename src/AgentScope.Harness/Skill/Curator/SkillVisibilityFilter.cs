// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

