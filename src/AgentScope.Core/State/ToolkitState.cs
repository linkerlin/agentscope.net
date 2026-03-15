// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.State;

/// <summary>
/// 工具集状态，用于持久化当前激活的工具组等。
/// </summary>
public record ToolkitState(IReadOnlySet<string> ActiveGroups) : IState
{
    public static ToolkitState Empty => new ToolkitState(new HashSet<string>());
}
