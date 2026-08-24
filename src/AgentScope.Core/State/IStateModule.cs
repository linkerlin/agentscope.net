// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using SessionStore = AgentScope.Core.Session.Session;

namespace AgentScope.Core.State;

/// <summary>
/// Module interface that supports state persistence. Accesses state through Session.Context with minimal invasiveness (does not rewrite Session).
/// 支持状态持久化的模块接口。通过 Session.Context 存取，实现最小侵入（不重写 Session）。
/// </summary>
public interface IStateModule
{
    /// <summary>
    /// Saves the current state into the session.
    /// 将当前状态写入 Session。
    /// </summary>
    /// <param name="session">Target session / 目标会话</param>
    /// <param name="sessionKey">Key under which to store the state / 存储状态的键</param>
    void SaveTo(SessionStore session, string sessionKey);

    /// <summary>
    /// Loads state from the session (throws or initializes if not present).
    /// 从 Session 加载状态（不存在则抛异常或初始化）。
    /// </summary>
    /// <param name="session">Source session / 源会话</param>
    /// <param name="sessionKey">Key from which to load the state / 加载状态的键</param>
    void LoadFrom(SessionStore session, string sessionKey);

    /// <summary>
    /// Loads state from the session if present; otherwise leaves current state unchanged.
    /// 若 Session 中存在则加载，否则不修改当前状态。
    /// </summary>
    /// <param name="session">Source session / 源会话</param>
    /// <param name="sessionKey">Key from which to load the state / 加载状态的键</param>
    void LoadIfExists(SessionStore session, string sessionKey);
}
