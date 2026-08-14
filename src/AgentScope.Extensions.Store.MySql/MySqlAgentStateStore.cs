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

using AgentScope.Extensions.Store;

namespace AgentScope.Extensions.Store.MySql;

/// <summary>
/// 基于 MySQL (JDBC 等价: MySqlDistributedStore) 的 Agent 状态存储。
/// 对应 Java: io.agentscope.extensions.mysql.state.MysqlAgentStateStore
/// </summary>
public sealed class MySqlAgentStateStore : DistributedAgentStateStore
{
    public MySqlAgentStateStore(MySqlDistributedStore store, string keyPrefix = "agentstate")
        : base(store, keyPrefix)
    {
    }
}
