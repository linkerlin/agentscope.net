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

namespace AgentScope.Extensions.Store.Redis;

/// <summary>
/// Redis-backed agent state store.
/// 基于 Redis 的 Agent 状态存储，用于持久化和查询 Agent 运行时状态。
/// </summary>
/// <remarks>
/// Corresponds to Java: io.agentscope.extensions.redis.state.RedisAgentStateStore
/// 对应 Java 实现：io.agentscope.extensions.redis.state.RedisAgentStateStore
/// The underlying <see cref="RedisDistributedStore"/> handles the actual Redis commands,
/// while this class provides typed agent-state semantics on top.
/// 底层 <see cref="RedisDistributedStore"/> 负责实际 Redis 命令，
/// 本类在其之上提供类型化的 Agent 状态语义。
/// </remarks>
public sealed class RedisAgentStateStore : DistributedAgentStateStore
{
    /// <summary>
    /// Initializes a new instance of <see cref="RedisAgentStateStore"/> with an existing <see cref="RedisDistributedStore"/>.
    /// 使用已有的 <see cref="RedisDistributedStore"/> 初始化状态存储。
    /// </summary>
    /// <param name="store">The underlying Redis distributed store instance / 底层 Redis 分布式存储实例。</param>
    /// <param name="keyPrefix">
    /// Prefix for all Redis keys managed by this store; defaults to "agentstate".
    /// 该存储管理的所有 Redis 键的前缀，默认为 "agentstate"。
    /// </param>
    public RedisAgentStateStore(RedisDistributedStore store, string keyPrefix = "agentstate")
        : base(store, keyPrefix)
    {
    }

    /// <summary>
    /// Convenience constructor: creates the underlying store from a Redis connection string.
    /// 便捷构造：直接传入 Redis 连接字符串，自动创建底层存储。
    /// </summary>
    /// <param name="connectionString">
    /// Redis connection string (e.g. "localhost:6379").
    /// Redis 连接字符串（例如 "localhost:6379"）。
    /// </param>
    /// <param name="keyPrefix">
    /// Prefix for all Redis keys managed by this store; defaults to "agentstate".
    /// 该存储管理的所有 Redis 键的前缀，默认为 "agentstate"。
    /// </param>
    public RedisAgentStateStore(string connectionString, string keyPrefix = "agentstate")
        : this(new RedisDistributedStore(connectionString), keyPrefix)
    {
    }
}
