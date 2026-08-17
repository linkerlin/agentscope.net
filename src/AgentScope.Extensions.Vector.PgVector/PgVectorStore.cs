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

using System.Data.Common;
using System.Runtime.CompilerServices;
using AgentScope.Extensions.Vector;
using Npgsql;

namespace AgentScope.Extensions.Vector.PgVector;

/// <summary>
/// PostgreSQL pgvector vector store adapter. Counterpart of Java PgVectorStore.
/// PostgreSQL pgvector 向量存储适配器。对标 Java PgVectorStore。
/// </summary>
public sealed class PgVectorStore(NpgsqlDataSource dataSource, string tableName, int dimension) : IVectorStore
{
    /// <summary>
    /// Gets the dimension of the vectors stored in this store.
    /// 获取此存储中向量的维度。
    /// </summary>
    public int Dimension => dimension;

    /// <summary>
    /// Upserts a vector entry into the specified pgvector table using an INSERT ... ON CONFLICT DO UPDATE statement.
    /// 使用 INSERT ... ON CONFLICT DO UPDATE 语句将向量条目写入（插入或更新）到指定 pgvector 表中。
    /// </summary>
    /// <param name="collection">Table name; falls back to tableName if null. 表名，为 null 时回退为 tableName。</param>
    /// <param name="id">Row ID. 行 ID。</param>
    /// <param name="vector">The float vector to store. 要存储的浮点向量。</param>
    /// <param name="payload">Optional JSON metadata payload. 可选的 JSON 元数据载荷。</param>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    /// <returns>A task representing the asynchronous operation. 表示异步操作的任务。</returns>
    public async ValueTask UpsertAsync(string collection, string id, float[] vector,
        IDictionary<string, object>? payload = null, CancellationToken ct = default)
    {
        // 构建 upsert SQL：插入 vector、payload 和时间戳，冲突时更新 vector 和 payload
        await using var cmd = dataSource.CreateCommand(
            $@"INSERT INTO {tableName} (id, vector, payload, created_at)
               VALUES ($1, $2::vector, $3::jsonb, NOW())
               ON CONFLICT (id) DO UPDATE SET vector = $2::vector, payload = $3::jsonb");
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(vector);
        // payload 序列化为 JSONB 格式
        cmd.Parameters.AddWithValue(System.Text.Json.JsonSerializer.Serialize(payload ?? new Dictionary<string, object>()));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <summary>
    /// Searches the top-K nearest neighbors for the given query vector using the cosine distance operator (&lt;=&gt;).
    /// 使用余弦距离运算符 (&lt;=&gt;) 查询向量的 top-K 最近邻。
    /// </summary>
    /// <param name="collection">Table name; falls back to tableName if null. 表名，为 null 时回退为 tableName。</param>
    /// <param name="query">The query float vector. 查询浮点向量。</param>
    /// <param name="topK">Number of nearest neighbors to return (default 5). 返回的最近邻数量（默认 5）。</param>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    /// <returns>An async enumerable of search hits. 搜索结果的异步可枚举序列。</returns>
    public async IAsyncEnumerable<SearchHit> SearchAsync(string collection, float[] query,
        int topK = 5, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 构建搜索 SQL：使用 <=> 余弦距离，score = 1 - distance，按距离升序排列
        await using var cmd = dataSource.CreateCommand(
            $@"SELECT id, 1 - (vector <=> $1::vector) AS score, payload
               FROM {tableName}
               ORDER BY vector <=> $1::vector
               LIMIT $2");
        cmd.Parameters.AddWithValue(query);
        cmd.Parameters.AddWithValue(topK);

        // 执行查询并逐行读取结果
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            yield return new SearchHit(
                reader.GetString(0),
                (float)reader.GetDouble(1));
        }
    }

    /// <summary>
    /// Disposes resources (no-op for this implementation).
    /// 释放资源（此实现为空操作）。
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
