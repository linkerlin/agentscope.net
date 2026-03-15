// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.State;

/// <summary>
/// Agent 元数据状态，用于持久化恢复 Agent 身份与系统提示。
/// </summary>
public record AgentMetaState(
    string Id,
    string Name,
    string Description,
    string SystemPrompt) : IState;
