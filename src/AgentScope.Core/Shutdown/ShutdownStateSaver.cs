// Copyright 2024-2026 the original author or authors.
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
/// 关闭状态持久化器：在优雅关闭时把 Agent 状态写入 IAgentStateStore，便于后续恢复。
/// 对应 Java: io.agentscope.core.shutdown.ShutdownStateSaver
/// </summary>
public class ShutdownStateSaver
{
    private readonly IAgentStateStore _store;

    public ShutdownStateSaver(IAgentStateStore store)
    {
        _store = store ?? throw new System.ArgumentNullException(nameof(store));
    }

    /// <summary>保存某个会话状态（关闭前调用）。</summary>
    public async Task SaveAsync(string userId, string sessionId, string key, AgentState state)
    {
        await _store.SaveAsync(userId, sessionId, key, state).ConfigureAwait(false);
    }

    /// <summary>关闭后加载状态用于恢复。</summary>
    public async Task<AgentState?> LoadAsync(string userId, string sessionId, string key)
    {
        return await _store.GetAsync(userId, sessionId, key).ConfigureAwait(false);
    }
}
