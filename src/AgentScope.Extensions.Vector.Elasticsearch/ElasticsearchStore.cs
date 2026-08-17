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
using AgentScope.Extensions.Vector;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace AgentScope.Extensions.Vector.Elasticsearch;

/// <summary>
/// Elasticsearch vector store adapter. Counterpart of Java ElasticsearchStore.
/// Elasticsearch 向量存储适配器。对标 Java ElasticsearchStore。
/// </summary>
public sealed class ElasticsearchStore(ElasticsearchClient client, string indexName, int dimension) : IVectorStore
{
    /// <summary>
    /// Gets the dimension of the vectors stored in this store.
    /// 获取此存储中向量的维度。
    /// </summary>
    public int Dimension => dimension;

    /// <summary>
    /// Upserts a vector entry into the specified collection.
    /// 将向量条目写入（插入或更新）到指定集合中。
    /// </summary>
    /// <param name="collection">Collection name; falls back to indexName if null. 集合名称，为 null 时回退为 indexName。</param>
    /// <param name="id">Document ID. 文档 ID。</param>
    /// <param name="vector">The float vector to store. 要存储的浮点向量。</param>
    /// <param name="payload">Optional metadata payload. 可选的元数据载荷。</param>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    /// <returns>A task representing the asynchronous operation. 表示异步操作的任务。</returns>
    public async ValueTask UpsertAsync(string collection, string id, float[] vector,
        IDictionary<string, object>? payload = null, CancellationToken ct = default)
    {
        // 确定目标索引名称
        var idx = collection ?? indexName;
        // 构建文档：向量 + ID + 可选的附加字段
        var doc = new Dictionary<string, object> { ["vector"] = vector, ["id"] = id };
        if (payload != null)
            foreach (var kv in payload) doc[kv.Key] = kv.Value;

        // 通过 Elasticsearch 客户端执行索引操作
        await client.IndexAsync(doc, idx, id, ct);
    }

    /// <summary>
    /// Searches the top-K nearest neighbors for the given query vector using k-NN search.
    /// 使用 k-NN 搜索查询向量的 top-K 最近邻。
    /// </summary>
    /// <param name="collection">Collection name; falls back to indexName if null. 集合名称，为 null 时回退为 indexName。</param>
    /// <param name="query">The query float vector. 查询浮点向量。</param>
    /// <param name="topK">Number of nearest neighbors to return (default 5). 返回的最近邻数量（默认 5）。</param>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    /// <returns>An async enumerable of search hits. 搜索结果的异步可枚举序列。</returns>
    public async IAsyncEnumerable<SearchHit> SearchAsync(string collection, float[] query,
        int topK = 5, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 确定目标索引名称
        var idx = collection ?? indexName;
        // 执行 k-NN 搜索，候选数为 topK * 10 以提升召回率
        var response = await client.SearchAsync<Dictionary<string, object>>(s => s
            .Index(idx)
            .Size(topK)
            .Query(q => q
                .Knn(k => k
                    .Field("vector")
                    .QueryVector(query)
                    .k(topK)
                    .NumCandidates(topK * 10))));

        // 遍历命中结果，优先使用 hit ID，回退到文档中的 id 字段
        foreach (var hit in response.Hits)
        {
            ct.ThrowIfCancellationRequested();
            yield return new SearchHit(
                hit.Id ?? hit.Source?.GetValueOrDefault("id")?.ToString() ?? "",
                (float)(hit.Score ?? 0));
        }
    }

    /// <summary>
    /// Disposes resources (no-op for this implementation).
    /// 释放资源（此实现为空操作）。
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
