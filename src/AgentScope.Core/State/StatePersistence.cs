// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.State;

/// <summary>
/// 状态持久化配置，指定哪些模块由 Session 管理持久化。
/// </summary>
public record StatePersistence(
    bool MemoryManaged = true,
    bool ToolkitManaged = true,
    bool PlanNotebookManaged = true)
{
    /// <summary>全部由 Session 管理</summary>
    public static StatePersistence All => new(true, true, true);

    /// <summary>不持久化任何模块</summary>
    public static StatePersistence None => new(false, false, false);
}
