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

namespace AgentScope.Extensions.Store.Redis;

/// <summary>
/// 基于 Redis 的 Agent 状态存储。
/// 对应 Java: io.agentscope.extensions.redis.state.RedisAgentStateStore
/// </summary>
public sealed class RedisAgentStateStore : DistributedAgentStateStore
{
    public RedisAgentStateStore(RedisDistributedStore store, string keyPrefix = "agentstate")
        : base(store, keyPrefix)
    {
    }

    /// <summary>便捷构造：直接传连接字符串。</summary>
    public RedisAgentStateStore(string connectionString, string keyPrefix = "agentstate")
        : this(new RedisDistributedStore(connectionString), keyPrefix)
    {
    }
}
