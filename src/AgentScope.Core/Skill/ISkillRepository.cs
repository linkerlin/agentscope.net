// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Skill;

/// <summary>
/// Skill 仓库：扫描得到 RegisteredSkill 列表，并按需加载为 ISkill。
/// </summary>
public interface ISkillRepository
{
    IEnumerable<RegisteredSkill> Scan();
    ISkill Load(RegisteredSkill registered);
}
