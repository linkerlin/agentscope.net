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

namespace AgentScope.Core.State;

/// <summary>
/// Agent state store interface supporting versioned optimistic concurrency control.
/// Agent 状态存储接口，支持版本化乐观并发控制。
/// Corresponds to Java: io.agentscope.core.state.AgentStateStore
/// 对应 Java: io.agentscope.core.state.AgentStateStore
/// </summary>
public interface IAgentStateStore
{
    /// <summary>
    /// Gets the state for the specified user, session and key.
    /// 获取指定用户、会话和键对应的状态。
    /// </summary>
    /// <param name="userId">User identifier / 用户标识</param>
    /// <param name="sessionId">Session identifier / 会话标识</param>
    /// <param name="key">State key / 状态键</param>
    /// <returns>The agent state, or null if not found / Agent 状态，未找到则返回 null</returns>
    Task<AgentState?> GetAsync(string userId, string sessionId, string key);

    /// <summary>
    /// Gets the versioned state for the specified user, session and key.
    /// 获取指定用户、会话和键对应的版本化状态。
    /// </summary>
    /// <param name="userId">User identifier / 用户标识</param>
    /// <param name="sessionId">Session identifier / 会话标识</param>
    /// <param name="key">State key / 状态键</param>
    /// <returns>The versioned state, or null if not found / 版本化状态，未找到则返回 null</returns>
    Task<VersionedState<AgentState>?> GetVersionedAsync(string userId, string sessionId, string key);

    /// <summary>
    /// Saves state unconditionally (overwrite).
    /// 保存状态（无条件覆盖）。
    /// </summary>
    /// <param name="userId">User identifier / 用户标识</param>
    /// <param name="sessionId">Session identifier / 会话标识</param>
    /// <param name="key">State key / 状态键</param>
    /// <param name="state">State to save / 要保存的状态</param>
    Task SaveAsync(string userId, string sessionId, string key, AgentState state);

    /// <summary>
    /// Versioned conditional save (CAS): writes only when the version matches.
    /// 版本化条件保存（CAS）：仅当版本匹配时写入。
    /// </summary>
    /// <param name="userId">User identifier / 用户标识</param>
    /// <param name="sessionId">Session identifier / 会话标识</param>
    /// <param name="key">State key / 状态键</param>
    /// <param name="state">State to save / 要保存的状态</param>
    /// <param name="expectedVersion">Expected version for CAS operation / CAS 操作的期望版本</param>
    /// <returns>The new version after successful write / 写入成功后的新版本号</returns>
    Task<long> SaveIfVersionAsync(string userId, string sessionId, string key, AgentState state, long expectedVersion);

    /// <summary>
    /// Sentinel value indicating an unversioned operation.
    /// 未版本化操作标记值。
    /// </summary>
    const long Unversioned = 0;

    /// <summary>
    /// Whether this store supports versioning.
    /// 是否支持版本化。
    /// </summary>
    bool SupportsVersioning { get; }
}

/// <summary>
/// Conflict resolution policy for concurrent state modifications.
/// 并发状态修改的冲突解决策略。
/// </summary>
public enum ConflictPolicy
{
    /// <summary>Overwrite directly without checking / 直接覆盖不校验</summary>
    Overwrite,

    /// <summary>Fail if version does not match / 版本不匹配则失败</summary>
    Fail,

    /// <summary>Append and merge states / 追加合并</summary>
    AppendMerge
}
