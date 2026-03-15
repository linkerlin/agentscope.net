// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

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
}
