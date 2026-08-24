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

using System.Runtime.CompilerServices;
using StackExchange.Redis;

namespace AgentScope.Extensions.Store.Redis;

/// <summary>
/// Redis-based implementation of <see cref="IDistributedStore"/> with async disposal support.
/// Redis 分布式存储实现，提供键值读写、删除、前缀列出以及异步释放能力。
/// </summary>
/// <remarks>
/// This store uses StackExchange.Redis to communicate with a Redis server.
/// All operations are asynchronous and support cancellation.
/// 该存储使用 StackExchange.Redis 与 Redis 服务器通信，所有操作均为异步且支持取消。
/// </remarks>
public sealed class RedisDistributedStore : IDistributedStore, IAsyncDisposable
{
    /// <summary>
    /// Multiplexer managing the underlying Redis connection pool.
    /// 管理底层 Redis 连接池的多路复用器。
    /// </summary>
    private readonly ConnectionMultiplexer _redis;

    /// <summary>
    /// Default database instance obtained from the multiplexer.
    /// 从多路复用器获取的默认数据库实例。
    /// </summary>
    private readonly IDatabase _db;

    /// <summary>
    /// Initializes a new instance of <see cref="RedisDistributedStore"/> from a connection string.
    /// 根据连接字符串初始化 Redis 分布式存储。
    /// </summary>
    /// <param name="connectionString">
    /// Redis connection string (e.g. "localhost:6379,password=xxx").
    /// Redis 连接字符串（例如 "localhost:6379,password=xxx"）。
    /// </param>
    /// <remarks>
    /// The connection is established synchronously at construction time.
    /// 构造函数中同步建立 Redis 连接。
    /// </remarks>
    public RedisDistributedStore(string connectionString)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _db = _redis.GetDatabase();
    }

    /// <summary>
    /// Retrieves the value associated with the specified key.
    /// 获取指定键关联的值。
    /// </summary>
    /// <param name="key">The key to look up / 要查找的键。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>
    /// The string value if the key exists; otherwise, <c>null</c>.
    /// 键存在时返回字符串值，否则返回 <c>null</c>。
    /// </returns>
    public async ValueTask<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var val = await _db.StringGetAsync(key);
        return val.HasValue ? val.ToString() : null;
    }

    /// <summary>
    /// Sets the value for the specified key with an optional TTL.
    /// 设置指定键的值，并可选择指定过期时间。
    /// </summary>
    /// <param name="key">The key to set / 要设置的键。</param>
    /// <param name="value">The value to store / 要存储的值。</param>
    /// <param name="ttl">
    /// Optional time-to-live duration; <c>null</c> means no expiration.
    /// 可选的过期时间；<c>null</c> 表示永不过期。
    /// </param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    public async ValueTask SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        await _db.StringSetAsync(key, value, ttl, When.Always, CommandFlags.None);
    }

    /// <summary>
    /// Deletes the specified key and returns whether the deletion succeeded.
    /// 删除指定的键，返回是否成功删除。
    /// </summary>
    /// <param name="key">The key to delete / 要删除的键。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>
    /// <c>true</c> if the key existed and was deleted; otherwise <c>false</c>.
    /// 键存在且被删除时返回 <c>true</c>，否则返回 <c>false</c>。
    /// </returns>
    public async ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default)
        => await _db.KeyDeleteAsync(key);

    /// <summary>
    /// Enumerates all keys with the given prefix via server-side SCAN.
    /// 通过服务器端 SCAN 命令枚举所有匹配给定前缀的键。
    /// </summary>
    /// <param name="prefix">
    /// Key prefix to filter by (e.g. "agentstate:").
    /// 用于过滤的键前缀（如 "agentstate:"）。
    /// </param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>
    /// An async-enumerable sequence of full key names.
    /// 完整键名的异步可枚举序列。
    /// </returns>
    /// <remarks>
    /// This method picks the first endpoint from the multiplexer.
    /// 本方法从多路复用器中选取第一个端点进行扫描。
    /// </remarks>
    public async IAsyncEnumerable<string> ListKeysAsync(string prefix, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 获取第一个可用的 Redis 服务器实例（适用于单节点或集群中的任一节点）
        // Get the first available Redis server instance (works with single node or any cluster node)
        var server = _redis.GetServer(_redis.GetEndPoints().First());

        // 使用 SCAN 模式遍历匹配前缀的所有键，避免阻塞性的 KEYS 命令
        // Use SCAN pattern to iterate keys matching the prefix, avoiding the blocking KEYS command
        await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
            yield return key.ToString();
    }

    /// <summary>
    /// Gracefully closes the Redis connection and releases all managed resources.
    /// 优雅关闭 Redis 连接并释放所有托管资源。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _redis.CloseAsync();
        _redis.Dispose();
    }
}
