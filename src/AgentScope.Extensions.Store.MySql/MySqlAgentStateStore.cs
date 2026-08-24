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

using AgentScope.Extensions.Store;

namespace AgentScope.Extensions.Store.MySql;

/// <summary>
/// MySQL-based Agent state store, backed by <see cref="MySqlDistributedStore"/>.
/// 基于 MySQL 的 Agent 状态存储，底层由 <see cref="MySqlDistributedStore"/> 提供分布式存储能力。
/// Equivalent to Java class: io.agentscope.extensions.mysql.state.MysqlAgentStateStore
/// 对应 Java 类：io.agentscope.extensions.mysql.state.MysqlAgentStateStore
/// </summary>
public sealed class MySqlAgentStateStore : DistributedAgentStateStore
{
    /// <summary>
    /// Initializes a new instance of <see cref="MySqlAgentStateStore"/>.
    /// 初始化 <see cref="MySqlAgentStateStore"/> 的新实例。
    /// </summary>
    /// <param name="store">The underlying MySQL distributed store / 底层的 MySQL 分布式存储实例</param>
    /// <param name="keyPrefix">
    /// Key prefix for scoping agent state entries; defaults to "agentstate".
    /// 用于隔离 Agent 状态条目的键前缀，默认为 "agentstate"。
    /// </param>
    public MySqlAgentStateStore(MySqlDistributedStore store, string keyPrefix = "agentstate")
        : base(store, keyPrefix)
    {
    }
}
