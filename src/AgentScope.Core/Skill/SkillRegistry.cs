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

using System.Collections.Concurrent;

namespace AgentScope.Core.Skill;

/// <summary>
/// Registry for managing skill lifecycle: registration, activation/deactivation, and retrieval by ID.
/// 技能注册表：管理技能的生命周期，包括注册、激活/停用以及按 ID 获取。
/// Corresponds to Java: io.agentscope.core.skill.SkillRegistry
/// </summary>
public class SkillRegistry
{
    /// <summary>
    /// Dictionary of loaded runtime skills, keyed by skill ID (case-insensitive).
    /// 已加载的运行时技能字典，以技能 ID 为键（不区分大小写）。
    /// </summary>
    private readonly ConcurrentDictionary<string, ISkill> _skills = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Dictionary of registered skill metadata, keyed by skill ID (case-insensitive).
    /// 已注册的技能元数据字典，以技能 ID 为键（不区分大小写）。
    /// </summary>
    private readonly ConcurrentDictionary<string, RegisteredSkill> _registered = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a skill with the given ID and optional metadata.
    /// 使用给定的 ID 和可选的元数据注册一个技能。
    /// </summary>
    /// <param name="skillId">The skill ID. / 技能 ID。</param>
    /// <param name="skill">The runtime skill instance. / 运行时技能实例。</param>
    /// <param name="metadata">Optional registration metadata. / 可选的注册元数据。</param>
    /// <exception cref="ArgumentNullException">Thrown when skillId or skill is null. / 当 skillId 或 skill 为 null 时抛出。</exception>
    public void Register(string skillId, ISkill skill, RegisteredSkill? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(skillId)) throw new ArgumentNullException(nameof(skillId));
        if (skill == null) throw new ArgumentNullException(nameof(skill));
        _skills[skillId.Trim()] = skill;
        if (metadata != null)
            _registered[skillId.Trim()] = metadata;
    }

    /// <summary>
    /// Sets the active state of a skill by ID.
    /// 根据 ID 设置技能的激活状态。
    /// </summary>
    /// <param name="skillId">The skill ID. / 技能 ID。</param>
    /// <param name="active">Whether the skill should be active. / 技能是否应激活。</param>
    public void SetActive(string skillId, bool active)
    {
        if (_skills.TryGetValue(skillId ?? "", out var s))
            s.IsActive = active;
    }

    /// <summary>
    /// Gets a loaded skill by ID.
    /// 根据 ID 获取已加载的技能。
    /// </summary>
    /// <param name="skillId">The skill ID. / 技能 ID。</param>
    /// <returns>The skill instance, or null if not found. / 技能实例，如果未找到则返回 null。</returns>
    public ISkill? Get(string skillId)
    {
        return _skills.TryGetValue(skillId ?? "", out var s) ? s : null;
    }

    /// <summary>
    /// Gets all currently active skills.
    /// 获取所有当前激活的技能。
    /// </summary>
    /// <returns>An enumerable of active skills. / 激活技能的可枚举集合。</returns>
    public IEnumerable<ISkill> GetActiveSkills()
    {
        return _skills.Values.Where(x => x.IsActive);
    }

    /// <summary>
    /// Gets registered skill metadata by ID.
    /// 根据 ID 获取已注册的技能元数据。
    /// </summary>
    /// <param name="skillId">The skill ID. / 技能 ID。</param>
    /// <returns>The registered skill metadata, or null if not found. / 已注册的技能元数据，如果未找到则返回 null。</returns>
    public RegisteredSkill? GetRegistered(string skillId)
    {
        return _registered.TryGetValue(skillId ?? "", out var r) ? r : null;
    }

    /// <summary>
    /// Returns a snapshot of all registered skill metadata.
    /// 返回所有已注册技能元数据的快照。
    /// </summary>
    /// <returns>A read-only collection of registered skill metadata. / 已注册技能元数据的只读集合。</returns>
    public IReadOnlyCollection<RegisteredSkill> ListSkills()
    {
        return _registered.Values.ToArray();
    }
}
