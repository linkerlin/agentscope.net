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

/// <summary>
/// Abstract base class for visibility filters targeting agent-created skills.
/// Subclasses decide how to mark/validate skills dynamically created by agents; the default implementation passes through skills not marked as agent-created.
/// 针对"Agent 自创建技能"的可见性过滤器抽象基类。
/// 子类决定如何标记/校验由 Agent 动态创建的技能；默认实现放行未被标记为 agent-created 的技能。
/// </summary>
public abstract class AbstractAgentCreatedFilter : ISkillVisibilityFilter
{
    /// <summary>
    /// Determines whether the skill was created by an agent (by default, checks if SourcePath falls under the agent-created directory).
    /// 判断技能是否由 Agent 创建（默认按 SourcePath 是否落在 agent-created 目录判断）。
    /// </summary>
    protected virtual bool IsAgentCreated(RegisteredSkill skill)
    {
        return skill?.SourcePath != null &&
               skill.SourcePath.Contains("agent-created", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Subclass implementation: specific filtering strategy for agent-created skills.
    /// 子类实现：对 agent-created 技能的具体过滤策略。
    /// </summary>
    /// <param name="skill">The skill to evaluate / 待评估的技能。</param>
    /// <param name="context">Current runtime context / 当前运行时上下文。</param>
    /// <returns>True if the agent-created skill should be allowed / 如果允许该 agent 创建则返回 true。</returns>
    protected abstract bool AllowAgentCreated(RegisteredSkill skill, RuntimeContext? context);

    /// <inheritdoc />
    public List<RegisteredSkill> Filter(List<RegisteredSkill> skills, RuntimeContext? context)
    {
        var result = new List<RegisteredSkill>();
        foreach (var skill in skills ?? new List<RegisteredSkill>())
        {
            if (!IsAgentCreated(skill))
            {
                result.Add(skill);
                continue;
            }

            if (AllowAgentCreated(skill, context))
            {
                result.Add(skill);
            }
        }

        return result;
    }
}

/// <summary>
/// Filter that allows all agent-created skills (for open policy scenarios).
/// 放行所有 Agent 创建技能的过滤器（用于开放策略场景）。
/// </summary>
public sealed class AllowAllAgentCreatedFilter : AbstractAgentCreatedFilter
{
    protected override bool AllowAgentCreated(RegisteredSkill skill, RuntimeContext? context) => true;
}

/// <summary>
/// Filter that denies all agent-created skills (for strict security policy scenarios).
/// 拒绝所有 Agent 创建技能的过滤器（用于严格安全策略场景）。
/// </summary>
public sealed class DenyAllAgentCreatedFilter : AbstractAgentCreatedFilter
{
    protected override bool AllowAgentCreated(RegisteredSkill skill, RuntimeContext? context) => false;
}
