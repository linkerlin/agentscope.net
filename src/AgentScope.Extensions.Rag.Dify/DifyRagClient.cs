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

namespace AgentScope.Extensions.Rag.Dify;

/// <summary>
/// Dify RAG client.
/// Dify 平台的 RAG（检索增强生成）客户端，用于检索数据集中的文档。
/// </summary>
public sealed class DifyRagClient
{
    /// <summary>
    /// HTTP client for API communication.
    /// 用于与 Dify API 通信的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _http;

    /// <summary>
    /// API key for Bearer authentication.
    /// API 密钥，用于 Bearer 身份认证。
    /// </summary>
    private readonly string _apiKey;

    /// <summary>
    /// Base URL of the Dify API.
    /// Dify API 的基础地址。
    /// </summary>
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of <see cref="DifyRagClient"/>.
    /// 初始化 <see cref="DifyRagClient"/> 类的新实例。
    /// </summary>
    /// <param name="http">The HTTP client instance / HTTP 客户端实例。</param>
    /// <param name="apiKey">The API key / API 密钥。</param>
    /// <param name="baseUrl">Optional custom base URL; defaults to Dify API / 可选的自定义基础地址，默认为 Dify API。</param>
    public DifyRagClient(HttpClient http, string apiKey, string? baseUrl = null)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://api.dify.ai/v1";
    }

    /// <summary>
    /// Retrieves relevant documents from a dataset.
    /// 从数据集中检索相关文档。
    /// </summary>
    /// <param name="datasetId">The dataset identifier / 数据集标识符。</param>
    /// <param name="query">The search query / 搜索查询文本。</param>
    /// <param name="topK">Maximum number of results to return / 返回的最大结果数量。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>A list of matching document texts / 匹配的文档文本列表。</returns>
    public async Task<List<string>> RetrieveAsync(string datasetId, string query, int topK = 5, CancellationToken ct = default)
    {
        // Build a request with Bearer token authentication
        // 使用 Bearer 令牌认证构建请求
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/datasets/{datasetId}/retrieve");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = JsonContent.Create(new { query, top_k = topK });
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var results = new List<string>();
        // Extract text from each returned document
        // 从每个返回的文档中提取文本
        foreach (var doc in json.GetProperty("documents").EnumerateArray())
            results.Add(doc.GetProperty("text").GetString() ?? "");
        return results;
    }
}
