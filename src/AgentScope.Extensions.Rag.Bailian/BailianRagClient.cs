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

namespace AgentScope.Extensions.Rag.Bailian;

/// <summary>
/// Bailian (Alibaba Cloud) RAG client.
/// 阿里云百炼平台的 RAG（检索增强生成）客户端，用于搜索索引和管理索引。
/// </summary>
public sealed class BailianRagClient
{
    /// <summary>
    /// HTTP client for API communication.
    /// 用于与百炼 RAG API 通信的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _http;

    /// <summary>
    /// API key for authentication.
    /// API 密钥，用于身份认证。
    /// </summary>
    private readonly string _apiKey;

    /// <summary>
    /// Base URL of the Bailian RAG API.
    /// 百炼 RAG API 的基础地址。
    /// </summary>
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of <see cref="BailianRagClient"/>.
    /// 初始化 <see cref="BailianRagClient"/> 类的新实例。
    /// </summary>
    /// <param name="http">The HTTP client instance / HTTP 客户端实例。</param>
    /// <param name="apiKey">The API key / API 密钥。</param>
    /// <param name="baseUrl">Optional custom base URL; defaults to Bailian RAG API / 可选的自定义基础地址，默认为百炼 RAG API。</param>
    public BailianRagClient(HttpClient http, string apiKey, string? baseUrl = null)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://bailian.aliyuncs.com/api/v1/rag";
    }

    /// <summary>
    /// Searches a specified index for relevant documents.
    /// 在指定索引中搜索相关文档。
    /// </summary>
    /// <param name="indexId">The index identifier / 索引标识符。</param>
    /// <param name="query">The search query / 搜索查询文本。</param>
    /// <param name="topK">Maximum number of results to return / 返回的最大结果数量。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>A list of matching document texts / 匹配的文档文本列表。</returns>
    public async Task<List<string>> SearchAsync(string indexId, string query, int topK = 5, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/indexes/{indexId}/search", new { query, top_k = topK }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var results = new List<string>();
        // Extract text from each result item
        // 从每个结果项中提取文本
        foreach (var item in json.GetProperty("results").EnumerateArray())
            results.Add(item.GetProperty("text").GetString() ?? "");
        return results;
    }

    /// <summary>
    /// Creates a new RAG index.
    /// 创建一个新的 RAG 索引。
    /// </summary>
    /// <param name="name">The index name / 索引名称。</param>
    /// <param name="description">The index description / 索引描述。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>The index ID assigned by the API / API 返回的索引 ID。</returns>
    public async Task<string> CreateIndexAsync(string name, string description, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/indexes", new { name, description }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("index_id").GetString() ?? "";
    }
}
