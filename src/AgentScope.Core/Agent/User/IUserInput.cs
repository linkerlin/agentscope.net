// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Threading.Tasks;

namespace AgentScope.Core.Agent.User;

/// <summary>
/// 用户输入接口
/// 对应 Java: io.agentscope.core.agent.user.IUserInput
/// </summary>
public interface IUserInput
{
    Task<string> RequestAsync(string prompt);
}
