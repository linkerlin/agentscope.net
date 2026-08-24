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
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AgentScope.Extensions.Vector.Qdrant;

/// <summary>
/// Qdrant vector store adapter. Counterpart of Java QdrantStore.
/// The Qdrant.Client heavy dependency is isolated in this sub-project.
/// Qdrant 向量存储适配器。对标 Java QdrantStore。
/// 子工程隔离 Qdrant.Client 重依赖。
/// </summary>
public sealed class QdrantStore(QdrantClient client, int dimension) : IVectorStore
{
    /// <summary>
    /// Gets the dimension of the vectors stored in this store.
    /// 获取此存储中向量的维度。
    /// </summary>
    public int Dimension => dimension;

    /// <summary>
    /// Upserts a vector entry into the specified Qdrant collection.
    /// 将向量条目写入（插入或更新）到指定 Qdrant 集合中。
    /// </summary>
    /// <param name="collection">Collection name. 集合名称。</param>
    /// <param name="id">Point ID (parsed as ulong). 点 ID（解析为 ulong）。</param>
    /// <param name="vector">The float vector to store. 要存储的浮点向量。</param>
    /// <param name="payload">Optional metadata payload; values are converted to Qdrant Value types. 可选的元数据载荷，值将被转换为 Qdrant Value 类型。</param>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    /// <returns>A task representing the asynchronous operation. 表示异步操作的任务。</returns>
    public async ValueTask UpsertAsync(string collection, string id, float[] vector,
        IDictionary<string, object>? payload = null, CancellationToken ct = default)
    {
        var point = new PointStruct
        {
            Id = ulong.Parse(id),
            Vectors = vector
        };

        // 将 payload 字典中的值转换为 Qdrant gRPC Value 类型
        if (payload != null)
        {
            foreach (var kv in payload)
            {
                var qValue = new Value();
                if (kv.Value is string s) qValue.StringValue = s;
                else if (kv.Value is long l) qValue.IntegerValue = l;
                else if (kv.Value is double d) qValue.DoubleValue = d;
                else if (kv.Value is bool b) qValue.BoolValue = b;
                else qValue.StringValue = kv.Value?.ToString() ?? "";
                point.Payload.Add(kv.Key, qValue);
            }
        }

        // 调用 Qdrant gRPC Upsert 接口
        await client.UpsertAsync(collection, [point], cancellationToken: ct);
    }

    /// <summary>
    /// Searches the top-K nearest neighbors for the given query vector.
    /// 查询向量的 top-K 最近邻。
    /// </summary>
    /// <param name="collection">Collection name. 集合名称。</param>
    /// <param name="query">The query float vector. 查询浮点向量。</param>
    /// <param name="topK">Number of nearest neighbors to return (default 5). 返回的最近邻数量（默认 5）。</param>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    /// <returns>An async enumerable of search hits with payload. 包含 payload 的搜索结果异步可枚举序列。</returns>
    public async IAsyncEnumerable<SearchHit> SearchAsync(string collection, float[] query,
        int topK = 5, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 执行 Qdrant gRPC 搜索
        var results = await client.SearchAsync(collection, query, limit: (ulong)topK, cancellationToken: ct);
        foreach (var p in results)
        {
            // 将 Qdrant gRPC Value 类型转换回 .NET 原生类型
            var dict = new Dictionary<string, object>();
            foreach (var kv in p.Payload)
            {
                object val = kv.Value.HasStringValue ? kv.Value.StringValue :
                    kv.Value.HasIntegerValue ? (object)kv.Value.IntegerValue :
                    kv.Value.HasDoubleValue ? (object)kv.Value.DoubleValue :
                    kv.Value.HasBoolValue ? (object)kv.Value.BoolValue :
                    kv.Value.ToString();
                dict[kv.Key] = val;
            }
            yield return new SearchHit(p.Id.ToString(), p.Score, dict);
        }
    }

    /// <summary>
    /// Disposes resources (no-op for this implementation).
    /// 释放资源（此实现为空操作）。
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
