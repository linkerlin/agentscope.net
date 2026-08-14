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
/// Agent 状态存储接口，支持版本化乐观并发控制
/// 对应 Java: io.agentscope.core.state.AgentStateStore
/// </summary>
public interface IAgentStateStore
{
    /// <summary>
    /// 获取状态
    /// </summary>
    Task<AgentState?> GetAsync(string userId, string sessionId, string key);

    /// <summary>
    /// 获取版本化状态
    /// </summary>
    Task<VersionedState<AgentState>?> GetVersionedAsync(string userId, string sessionId, string key);

    /// <summary>
    /// 保存状态（无条件覆盖）
    /// </summary>
    Task SaveAsync(string userId, string sessionId, string key, AgentState state);

    /// <summary>
    /// 版本化条件保存（CAS）：仅当版本匹配时写入
    /// </summary>
    Task<long> SaveIfVersionAsync(string userId, string sessionId, string key, AgentState state, long expectedVersion);

    /// <summary>
    /// 未版本化的标记
    /// </summary>
    const long Unversioned = 0;

    /// <summary>
    /// 是否支持版本化
    /// </summary>
    bool SupportsVersioning { get; }
}

/// <summary>
/// 冲突策略
/// </summary>
public enum ConflictPolicy
{
    /// <summary>直接覆盖</summary>
    Overwrite,

    /// <summary>版本不匹配则失败</summary>
    Fail,

    /// <summary>追加合并</summary>
    AppendMerge
}
