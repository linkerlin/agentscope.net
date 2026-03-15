// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tool;

namespace AgentScope.Core.Skill;

/// <summary>
/// Skill 接口：复合功能单元，可包含多个 Tool，支持动态激活。
/// </summary>
public interface ISkill
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    IReadOnlyList<ITool> Tools { get; }
    bool IsActive { get; set; }
}
