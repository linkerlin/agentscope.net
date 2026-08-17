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

using System.Threading.Tasks;
using AgentScope.Core.State;

namespace AgentScope.Core.Shutdown;

/// <summary>
/// Shutdown state persister: writes Agent state to IAgentStateStore during graceful shutdown
/// so it can be recovered later.
/// 关闭状态持久化器：在优雅关闭时把 Agent 状态写入 IAgentStateStore，便于后续恢复。
/// Corresponds to Java: io.agentscope.core.shutdown.ShutdownStateSaver
/// </summary>
public class ShutdownStateSaver
{
    /// <summary>
    /// The underlying state store for persistence.
    /// 底层状态存储，用于持久化。
    /// </summary>
    private readonly IAgentStateStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShutdownStateSaver"/> class.
    /// 初始化 <see cref="ShutdownStateSaver"/> 类的新实例。
    /// </summary>
    /// <param name="store">The state store implementation. / 状态存储实现。</param>
    /// <exception cref="System.ArgumentNullException">Thrown when store is null. / 当 store 为 null 时抛出。</exception>
    public ShutdownStateSaver(IAgentStateStore store)
    {
        _store = store ?? throw new System.ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Saves the session state before shutdown.
    /// 保存某个会话状态（关闭前调用）。
    /// </summary>
    /// <param name="userId">The user identifier. / 用户标识符。</param>
    /// <param name="sessionId">The session identifier. / 会话标识符。</param>
    /// <param name="key">The state key. / 状态键。</param>
    /// <param name="state">The agent state to save. / 要保存的 Agent 状态。</param>
    public async Task SaveAsync(string userId, string sessionId, string key, AgentState state)
    {
        await _store.SaveAsync(userId, sessionId, key, state).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads the previously saved state for recovery after shutdown.
    /// 关闭后加载状态用于恢复。
    /// </summary>
    /// <param name="userId">The user identifier. / 用户标识符。</param>
    /// <param name="sessionId">The session identifier. / 会话标识符。</param>
    /// <param name="key">The state key. / 状态键。</param>
    /// <returns>The loaded agent state, or null if not found. / 加载的 Agent 状态，未找到则返回 null。</returns>
    public async Task<AgentState?> LoadAsync(string userId, string sessionId, string key)
    {
        return await _store.GetAsync(userId, sessionId, key).ConfigureAwait(false);
    }
}
