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

using System.Net.Http.Json;
using System.Text.Json;

namespace AgentScope.Extensions.Rag.Haystack;

/// <summary>
/// Haystack RAG client.
/// Haystack 框架的 RAG（检索增强生成）客户端，用于查询管道和索引文档。
/// </summary>
public sealed class HaystackRagClient
{
    /// <summary>
    /// HTTP client for API communication.
    /// 用于与 Haystack API 通信的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _http;

    /// <summary>
    /// Base URL of the Haystack API (trailing slash trimmed).
    /// Haystack API 的基础地址（已去除尾部斜杠）。
    /// </summary>
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of <see cref="HaystackRagClient"/>.
    /// 初始化 <see cref="HaystackRagClient"/> 类的新实例。
    /// </summary>
    /// <param name="http">The HTTP client instance / HTTP 客户端实例。</param>
    /// <param name="baseUrl">The base URL of the Haystack API / Haystack API 的基础地址。</param>
    public HaystackRagClient(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Queries a Haystack pipeline for relevant documents.
    /// 查询 Haystack 管道以获取相关文档。
    /// </summary>
    /// <param name="pipelineId">The pipeline identifier / 管道标识符。</param>
    /// <param name="query">The search query / 搜索查询文本。</param>
    /// <param name="topK">Maximum number of results to return / 返回的最大结果数量。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>A list of matching document contents / 匹配的文档内容列表。</returns>
    public async Task<List<string>> QueryAsync(string pipelineId, string query, int topK = 5, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/pipelines/{pipelineId}/query", new { query, top_k = topK }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var results = new List<string>();
        // Extract content from each document in the response
        // 从响应中的每个文档提取内容
        foreach (var doc in json.GetProperty("documents").EnumerateArray())
            results.Add(doc.GetProperty("content").GetString() ?? "");
        return results;
    }

    /// <summary>
    /// Indexes a document into a Haystack pipeline.
    /// 将文档索引到 Haystack 管道中。
    /// </summary>
    /// <param name="pipelineId">The pipeline identifier / 管道标识符。</param>
    /// <param name="text">The document text content / 文档文本内容。</param>
    /// <param name="docId">Optional custom document ID; a new GUID is used if not provided / 可选的自定义文档 ID，未提供时使用新的 GUID。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>The document ID / 文档 ID。</returns>
    public async Task<string> IndexDocumentAsync(string pipelineId, string text, string? docId = null, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/pipelines/{pipelineId}/documents", new { id = docId ?? Guid.NewGuid().ToString(), content = text }, ct);
        resp.EnsureSuccessStatusCode();
        return docId ?? "";
    }
}
