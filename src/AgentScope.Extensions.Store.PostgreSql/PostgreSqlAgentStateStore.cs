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

namespace AgentScope.Extensions.Store.PostgreSql;

/// <summary>
/// PostgreSQL-based agent state store.
/// Provides durable persistence of agent runtime state (conversation context, variables, etc.)
/// using a PostgreSQL backend, leveraging the distributed store infrastructure.
/// <br/>
/// 基于 PostgreSQL 的 Agent 状态存储。
/// 使用 PostgreSQL 后端提供 Agent 运行时状态（会话上下文、变量等）的持久化存储，
/// 底层依赖分布式存储基础设施实现。
/// <br/>
/// Corresponds to Java: io.agentscope.extensions.postgresql.state.PostgresAgentStateStore
/// 对应 Java 实现: io.agentscope.extensions.postgresql.state.PostgresAgentStateStore
/// </summary>
public sealed class PostgreSqlAgentStateStore : DistributedAgentStateStore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlAgentStateStore"/> class.
    /// <br/>
    /// 初始化 <see cref="PostgreSqlAgentStateStore"/> 类的新实例。
    /// </summary>
    /// <param name="store">
    /// The underlying PostgreSQL distributed store used for data persistence.
    /// 底层 PostgreSQL 分布式存储实例，用于数据持久化操作。
    /// </param>
    /// <param name="keyPrefix">
    /// Optional prefix for all state keys to avoid collisions; defaults to "agentstate".
    /// 所有状态键的可选前缀，用于避免键冲突；默认值为 "agentstate"。
    /// </param>
    public PostgreSqlAgentStateStore(PostgreSqlDistributedStore store, string keyPrefix = "agentstate")
        : base(store, keyPrefix)
    {
    }
}
