// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using SessionStore = AgentScope.Core.Session.Session;

namespace AgentScope.Core.Tool.SubAgent;

/// <summary>
/// SubAgent 配置：持有 Session，用于状态持久化与恢复。
/// </summary>
public class SubAgentConfig
{
    public SessionStore Session { get; }

    public SubAgentConfig(SessionStore session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }
}
