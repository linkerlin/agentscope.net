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
/// Skill 注册表：注册、激活/禁用、按 Id 获取。
/// </summary>
public class SkillRegistry
{
    private readonly ConcurrentDictionary<string, ISkill> _skills = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, RegisteredSkill> _registered = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string skillId, ISkill skill, RegisteredSkill? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(skillId)) throw new ArgumentNullException(nameof(skillId));
        if (skill == null) throw new ArgumentNullException(nameof(skill));
        _skills[skillId.Trim()] = skill;
        if (metadata != null)
            _registered[skillId.Trim()] = metadata;
    }

    public void SetActive(string skillId, bool active)
    {
        if (_skills.TryGetValue(skillId ?? "", out var s))
            s.IsActive = active;
    }

    public ISkill? Get(string skillId)
    {
        return _skills.TryGetValue(skillId ?? "", out var s) ? s : null;
    }

    public IEnumerable<ISkill> GetActiveSkills()
    {
        return _skills.Values.Where(x => x.IsActive);
    }

    public RegisteredSkill? GetRegistered(string skillId)
    {
        return _registered.TryGetValue(skillId ?? "", out var r) ? r : null;
    }

    /// <summary>返回所有已注册技能元数据的快照。</summary>
    public IReadOnlyCollection<RegisteredSkill> ListSkills()
    {
        return _registered.Values.ToArray();
    }
}
