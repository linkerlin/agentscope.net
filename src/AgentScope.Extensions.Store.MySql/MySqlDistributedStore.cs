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
using MySqlConnector;

namespace AgentScope.Extensions.Store.MySql;

/// <summary>
/// A distributed key-value store backed by MySQL, implementing <see cref="IDistributedStore"/>.
/// 基于 MySQL 的分布式键值存储，实现 <see cref="IDistributedStore"/> 接口。
/// Uses a dedicated table <c>agentscope_store</c> to persist entries with optional TTL-based expiration.
/// 使用专用表 <c>agentscope_store</c> 持久化条目，支持可选的 TTL 过期机制。
/// </summary>
public sealed class MySqlDistributedStore : IDistributedStore
{
    /// <summary>
    /// Connection string used to connect to the MySQL server.
    /// 用于连接 MySQL 服务器的连接字符串。
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of <see cref="MySqlDistributedStore"/>.
    /// 初始化 <see cref="MySqlDistributedStore"/> 的新实例。
    /// Automatically ensures the underlying table exists before returning.
    /// 在返回前自动确保底层数据表存在。
    /// </summary>
    /// <param name="connectionString">MySQL connection string / MySQL 连接字符串</param>
    public MySqlDistributedStore(string connectionString)
    {
        _connectionString = connectionString;
        // 同步等待表创建完成，确保后续操作可用
        // Block synchronously to guarantee the table is ready for subsequent operations
        EnsureTableAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates the <c>agentscope_store</c> table if it does not already exist.
    /// 如果 <c>agentscope_store</c> 表不存在则创建它。
    /// Schema includes key (primary key), value, creation timestamp, and optional expiration timestamp.
    /// 表结构包含键（主键）、值、创建时间戳和可选的过期时间戳。
    /// </summary>
    private async Task EnsureTableAsync()
    {
        // 打开连接并创建表
        // Open connection and create table
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS agentscope_store (
    `key` VARCHAR(255) PRIMARY KEY,
    `value` LONGTEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NULL
)";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Retrieves the value associated with the specified key.
    /// 获取指定键关联的值。
    /// Automatically excludes expired entries (those whose <c>expires_at</c> is in the past).
    /// 自动排除已过期的条目（<c>expires_at</c> 早于当前时间的条目）。
    /// </summary>
    /// <param name="key">The lookup key / 查询键</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>
    /// The value string if found and not expired; otherwise <c>null</c>.
    /// 如果找到且未过期则返回值字符串；否则返回 <c>null</c>。
    /// </returns>
    public async ValueTask<string?> GetAsync(string key, CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        // 只查询未过期的条目：expires_at 为 NULL（永不过期）或大于当前时间
        // Only query entries that haven't expired: expires_at is NULL (never expires) or later than NOW()
        cmd.CommandText = "SELECT `value` FROM agentscope_store WHERE `key` = @k AND (expires_at IS NULL OR expires_at > NOW())";
        cmd.Parameters.AddWithValue("@k", key);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result?.ToString();
    }

    /// <summary>
    /// Sets the value for the specified key, with an optional TTL (time-to-live).
    /// 设置指定键的值，支持可选的 TTL（生存时间）。
    /// Uses MySQL <c>REPLACE INTO</c> to insert or update the entry.
    /// 使用 MySQL 的 <c>REPLACE INTO</c> 进行插入或更新。
    /// </summary>
    /// <param name="key">The key to write / 要写入的键</param>
    /// <param name="value">The value to store / 要存储的值</param>
    /// <param name="ttl">
    /// Optional expiration duration. If <c>null</c>, the entry never expires.
    /// 可选的过期时长。如果为 <c>null</c>，条目永不过期。
    /// </param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async ValueTask SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        // 根据是否有 TTL 选择不同的 SQL，有 TTL 时计算绝对过期时间
        // Choose different SQL based on TTL presence; when TTL is set, compute the absolute expiration timestamp
        cmd.CommandText = ttl.HasValue
            ? "REPLACE INTO agentscope_store (`key`, `value`, `expires_at`) VALUES (@k, @v, DATE_ADD(NOW(), INTERVAL @t SECOND))"
            : "REPLACE INTO agentscope_store (`key`, `value`) VALUES (@k, @v)";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        if (ttl.HasValue) cmd.Parameters.AddWithValue("@t", (int)ttl.Value.TotalSeconds);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Deletes the entry associated with the specified key.
    /// 删除与指定键关联的条目。
    /// </summary>
    /// <param name="key">The key to delete / 要删除的键</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>
    /// <c>true</c> if an entry was deleted; <c>false</c> if the key did not exist.
    /// 如果删除了条目则返回 <c>true</c>；如果键不存在则返回 <c>false</c>。
    /// </returns>
    public async ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM agentscope_store WHERE `key` = @k";
        cmd.Parameters.AddWithValue("@k", key);
        // 影响行数 > 0 表示成功删除了条目
        // Affected rows > 0 indicates a row was actually deleted
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    /// <summary>
    /// Lists all keys that start with the given prefix.
    /// 列出所有以指定前缀开头的键。
    /// Uses a MySQL <c>LIKE</c> query with the prefix pattern.
    /// 使用 MySQL 的 <c>LIKE</c> 查询配合前缀模式进行匹配。
    /// </summary>
    /// <param name="prefix">The key prefix to filter by / 用于过滤的键前缀</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>
    /// An async-enumerable sequence of matching key strings.
    /// 一个异步可枚举序列，包含所有匹配的键字符串。
    /// </returns>
    public async IAsyncEnumerable<string> ListKeysAsync(string prefix, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT `key` FROM agentscope_store WHERE `key` LIKE @p";
        cmd.Parameters.AddWithValue("@p", $"{prefix}%");
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        // 逐行读取结果集，yield 返回每个键
        // Read the result set row by row and yield each key
        while (await reader.ReadAsync(ct))
            yield return reader.GetString(0);
    }
}
