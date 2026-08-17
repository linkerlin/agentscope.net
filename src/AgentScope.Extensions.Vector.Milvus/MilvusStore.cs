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

namespace AgentScope.Extensions.Vector.Milvus;

/// <summary>
/// Milvus vector store adapter (HTTP API implementation). Counterpart of Java MilvusStore.
/// Interacts via the Milvus RESTful API to avoid gRPC SDK version compatibility issues.
/// Milvus 向量存储适配器（HTTP API 实现）。对标 Java MilvusStore。
/// 通过 Milvus RESTful API 交互（不依赖 gRPC SDK 版本兼容性）。
/// </summary>
public sealed class MilvusStore(HttpClient httpClient, string baseUrl, string collectionName, int dimension) : IVectorStore
{
    /// <summary>
    /// Gets the dimension of the vectors stored in this store.
    /// 获取此存储中向量的维度。
    /// </summary>
    public int Dimension => dimension;

    /// <summary>
    /// Upserts a vector entry into the specified Milvus collection via the HTTP insert API.
    /// 通过 HTTP insert API 将向量条目写入（插入或更新）到指定 Milvus 集合中。
    /// </summary>
    /// <param name="collection">Collection name; falls back to collectionName if null. 集合名称，为 null 时回退为 collectionName。</param>
    /// <param name="id">Entity ID. 实体 ID。</param>
    /// <param name="vector">The float vector to store. 要存储的浮点向量。</param>
    /// <param name="payload">Optional metadata fields. 可选的元数据字段。</param>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    /// <returns>A task representing the asynchronous operation. 表示异步操作的任务。</returns>
    public async ValueTask UpsertAsync(string collection, string id, float[] vector,
        IDictionary<string, object>? payload = null, CancellationToken ct = default)
    {
        // 确定目标集合名称
        var col = collection ?? collectionName;
        // 构建字段数据：id + vector + 可选的附加字段
        var fields = new Dictionary<string, object> { ["id"] = id, ["vector"] = vector };
        if (payload != null)
            foreach (var kv in payload) fields[kv.Key] = kv.Value;

        // 序列化为 Milvus HTTP API 所需的请求体格式
        var body = new { collectionName = col, fieldsData = new[] { fields } };
        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // 调用 POST /v1/vector/insert 接口
        using var resp = await httpClient.PostAsync($"{baseUrl}/v1/vector/insert", content, ct);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Searches the top-K nearest neighbors for the given query vector via the Milvus HTTP search API.
    /// 通过 Milvus HTTP search API 查询向量的 top-K 最近邻。
    /// </summary>
    /// <param name="collection">Collection name; falls back to collectionName if null. 集合名称，为 null 时回退为 collectionName。</param>
    /// <param name="query">The query float vector. 查询浮点向量。</param>
    /// <param name="topK">Number of nearest neighbors to return (default 5). 返回的最近邻数量（默认 5）。</param>
    /// <param name="ct">Cancellation token. 取消令牌。</param>
    /// <returns>An async enumerable of search hits. 搜索结果的异步可枚举序列。</returns>
    public async IAsyncEnumerable<SearchHit> SearchAsync(string collection, float[] query,
        int topK = 5, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 确定目标集合名称
        var col = collection ?? collectionName;
        // 构建搜索请求体
        var body = new
        {
            collectionName = col,
            vector = query,
            limit = topK,
            outputFields = new[] { "id" }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // 调用 POST /v1/vector/search 接口
        using var resp = await httpClient.PostAsync($"{baseUrl}/v1/vector/search", content, ct);
        resp.EnsureSuccessStatusCode();

        // 解析返回的 JSON，提取结果数组
        var resultJson = await resp.Content.ReadAsStringAsync(ct);
        var doc = System.Text.Json.JsonDocument.Parse(resultJson);
        if (doc.RootElement.TryGetProperty("results", out var results))
        {
            // 遍历每个结果，提取 id 和 distance 分数
            foreach (var item in results.EnumerateArray())
            {
                ct.ThrowIfCancellationRequested();
                var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                var score = item.TryGetProperty("distance", out var dEl) ? (float)dEl.GetDouble() : 0;
                yield return new SearchHit(id, score);
            }
        }
    }

    /// <summary>
    /// Disposes resources (no-op for this implementation).
    /// 释放资源（此实现为空操作）。
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
