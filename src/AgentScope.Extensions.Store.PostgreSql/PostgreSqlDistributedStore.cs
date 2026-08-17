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
using Npgsql;

namespace AgentScope.Extensions.Store.PostgreSql;

/// <summary>
/// PostgreSQL-based implementation of <see cref="IDistributedStore"/>.
/// Provides distributed key-value storage with optional TTL-based expiration,
/// leveraging a dedicated <c>agentscope_store</c> table for persistence.
/// Suitable for multi-instance deployments requiring shared state across nodes.
/// <br/>
/// <see cref="IDistributedStore"/> 的 PostgreSQL 实现。
/// 利用专用的 <c>agentscope_store</c> 表提供分布式键值存储，支持可选的 TTL 过期机制。
/// 适用于需要跨节点共享状态的多实例部署场景。
/// </summary>
public sealed class PostgreSqlDistributedStore : IDistributedStore
{
    /// <summary>
    /// PostgreSQL connection string used to connect to the database.
    /// PostgreSQL 连接字符串，用于连接到数据库。
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDistributedStore"/> class.
    /// Automatically ensures the backing table exists on construction.
    /// <br/>
    /// 初始化 <see cref="PostgreSqlDistributedStore"/> 类的新实例。
    /// 构造时自动确保底层数据表已创建。
    /// </summary>
    /// <param name="connectionString">
    /// PostgreSQL connection string (e.g., "Host=localhost;Database=agentscope;Username=...").
    /// PostgreSQL 连接字符串（例如 "Host=localhost;Database=agentscope;Username=..."）。
    /// </param>
    public PostgreSqlDistributedStore(string connectionString)
    {
        _connectionString = connectionString;
        // 同步等待表创建完成，确保后续操作可用
        // Synchronously wait for table creation to ensure availability for subsequent operations
        EnsureTableAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Ensures the <c>agentscope_store</c> table exists in the database.
    /// Creates it if it does not already exist, using an idempotent CREATE TABLE IF NOT EXISTS.
    /// <br/>
    /// 确保数据库中存在 <c>agentscope_store</c> 表。
    /// 若不存在则自动创建，使用幂等的 CREATE TABLE IF NOT EXISTS 语句。
    /// </summary>
    private async Task EnsureTableAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        // 建表 SQL：key 为主键，value 为必填，created_at 记录创建时间，expires_at 控制过期
        // Table schema: key as PK, value required, created_at tracks creation time, expires_at controls TTL
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS agentscope_store (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ NULL
)";
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Retrieves a value by key from the store.
    /// Automatically excludes expired entries (expires_at in the past).
    /// <br/>
    /// 根据键从存储中获取值。
    /// 自动排除已过期（expires_at 早于当前时间）的条目。
    /// </summary>
    /// <param name="key">
    /// The key to look up.
    /// 要查找的键。
    /// </param>
    /// <param name="ct">
    /// Cancellation token to cancel the operation.
    /// 用于取消操作的取消令牌。
    /// </param>
    /// <returns>
    /// The stored value as a string, or <c>null</c> if the key does not exist or has expired.
    /// 存储的值（字符串），若键不存在或已过期则返回 <c>null</c>。
    /// </returns>
    public async ValueTask<string?> GetAsync(string key, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        // 查询时过滤过期条目：expires_at 为 NULL 表示永不过期，否则必须大于当前时间
        // Filter expired rows: NULL expires_at means never expires, otherwise must be > NOW()
        await using var cmd = new NpgsqlCommand("SELECT value FROM agentscope_store WHERE key = @k AND (expires_at IS NULL OR expires_at > NOW())", conn);
        cmd.Parameters.AddWithValue("k", key);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result?.ToString();
    }

    /// <summary>
    /// Sets a key-value pair in the store with optional TTL-based expiration.
    /// Uses PostgreSQL UPSERT (INSERT … ON CONFLICT DO UPDATE) to create or overwrite.
    /// <br/>
    /// 在存储中设置键值对，支持可选的 TTL 过期时间。
    /// 使用 PostgreSQL 的 UPSERT（INSERT … ON CONFLICT DO UPDATE）实现创建或覆盖。
    /// </summary>
    /// <param name="key">
    /// The key to set.
    /// 要设置的键。
    /// </param>
    /// <param name="value">
    /// The value to store.
    /// 要存储的值。
    /// </param>
    /// <param name="ttl">
    /// Optional time-to-live; the entry will expire after this duration.
    /// Pass <c>null</c> for no expiration.
    /// 可选的生存时间；条目在此时间后过期。传递 <c>null</c> 表示永不过期。
    /// </param>
    /// <param name="ct">
    /// Cancellation token to cancel the operation.
    /// 用于取消操作的取消令牌。
    /// </param>
    public async ValueTask SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        // UPSERT 语句：若键已存在则更新 value 和 expires_at，否则插入新行
        // UPSERT: update value and expires_at if key exists, otherwise insert a new row
        await using var cmd = new NpgsqlCommand(@"
INSERT INTO agentscope_store (key, value, expires_at)
VALUES (@k, @v, @t)
ON CONFLICT (key) DO UPDATE SET value = @v, expires_at = @t", conn);
        cmd.Parameters.AddWithValue("k", key);
        cmd.Parameters.AddWithValue("v", value);
        // 若指定了 TTL，计算绝对过期时间；否则写入 DBNull（永不过期）
        // If TTL specified, compute absolute expiration time; otherwise write DBNull (never expires)
        cmd.Parameters.AddWithValue("t", ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Deletes a key-value pair from the store.
    /// <br/>
    /// 从存储中删除一个键值对。
    /// </summary>
    /// <param name="key">
    /// The key to delete.
    /// 要删除的键。
    /// </param>
    /// <param name="ct">
    /// Cancellation token to cancel the operation.
    /// 用于取消操作的取消令牌。
    /// </param>
    /// <returns>
    /// <c>true</c> if a row was deleted; <c>false</c> if the key did not exist.
    /// 若删除了行则返回 <c>true</c>；若键不存在则返回 <c>false</c>。
    /// </returns>
    public async ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("DELETE FROM agentscope_store WHERE key = @k", conn);
        cmd.Parameters.AddWithValue("k", key);
        // ExecuteNonQuery 返回受影响的行数，大于 0 表示成功删除
        // ExecuteNonQuery returns the number of affected rows; > 0 means a deletion occurred
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    /// <summary>
    /// Lists all keys that start with the specified prefix.
    /// Uses a LIKE query to perform prefix matching.
    /// <br/>
    /// 列出所有以指定前缀开头的键。
    /// 使用 LIKE 查询进行前缀匹配。
    /// </summary>
    /// <param name="prefix">
    /// The key prefix to match against.
    /// 要匹配的键前缀。
    /// </param>
    /// <param name="ct">
    /// Cancellation token to cancel the enumeration.
    /// 用于取消枚举操作的取消令牌。
    /// </param>
    /// <returns>
    /// An async-enumerable sequence of matching keys.
    /// 匹配键的异步可枚举序列。
    /// </returns>
    public async IAsyncEnumerable<string> ListKeysAsync(string prefix, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        // 使用 LIKE 'prefix%' 模式实现前缀匹配查询
        // Use LIKE 'prefix%' pattern for prefix-matching query
        await using var cmd = new NpgsqlCommand("SELECT key FROM agentscope_store WHERE key LIKE @p", conn);
        cmd.Parameters.AddWithValue("p", $"{prefix}%");
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        // 逐行流式读取结果集，避免一次性加载全部数据到内存
        // Stream-read the result set row by row to avoid loading all data into memory at once
        while (await reader.ReadAsync(ct))
            yield return reader.GetString(0);
    }
}
