// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Agent;

namespace AgentScope.Core.Tool.SubAgent;

/// <summary>
/// 子 Agent 提供者：按需创建 Agent 实例，供 SubAgentTool 调用。
/// </summary>
public interface ISubAgentProvider
{
    IAgent Provide();
}
