// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using SessionStore = AgentScope.Core.Session.Session;

namespace AgentScope.Core.State;

/// <summary>
/// 支持状态持久化的模块接口。通过 Session.Context 存取，实现最小侵入（不重写 Session）。
/// </summary>
public interface IStateModule
{
    /// <summary>将当前状态写入 Session</summary>
    void SaveTo(SessionStore session, string sessionKey);

    /// <summary>从 Session 加载状态（不存在则抛或初始化）</summary>
    void LoadFrom(SessionStore session, string sessionKey);

    /// <summary>若 Session 中存在则加载，否则不修改当前状态</summary>
    void LoadIfExists(SessionStore session, string sessionKey);
}
