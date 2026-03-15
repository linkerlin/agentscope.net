// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Skill;

/// <summary>
/// Skill 注册元数据（仓库扫描得到的条目，尚未加载为 ISkill）。
/// </summary>
public class RegisteredSkill
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? SourcePath { get; set; }
    public string? RawContent { get; set; }
}
