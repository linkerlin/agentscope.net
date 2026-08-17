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

namespace AgentScope.Extensions.Rag.RagFlow;

/// <summary>
/// RagFlow RAG client.
/// RagFlow 平台的 RAG（检索增强生成）客户端，用于搜索文档块和上传文档。
/// </summary>
public sealed class RagFlowRagClient
{
    /// <summary>
    /// HTTP client for API communication.
    /// 用于与 RagFlow API 通信的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _http;

    /// <summary>
    /// API key for Bearer authentication.
    /// API 密钥，用于 Bearer 身份认证。
    /// </summary>
    private readonly string _apiKey;

    /// <summary>
    /// Base URL of the RagFlow API.
    /// RagFlow API 的基础地址。
    /// </summary>
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of <see cref="RagFlowRagClient"/>.
    /// 初始化 <see cref="RagFlowRagClient"/> 类的新实例。
    /// </summary>
    /// <param name="http">The HTTP client instance / HTTP 客户端实例。</param>
    /// <param name="apiKey">The API key / API 密钥。</param>
    /// <param name="baseUrl">Optional custom base URL; defaults to RagFlow API / 可选的自定义基础地址，默认为 RagFlow API。</param>
    public RagFlowRagClient(HttpClient http, string apiKey, string? baseUrl = null)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://api.ragflow.io/v1";
    }

    /// <summary>
    /// Searches a dataset for relevant document chunks.
    /// 在数据集中搜索相关的文档块。
    /// </summary>
    /// <param name="datasetId">The dataset identifier / 数据集标识符。</param>
    /// <param name="query">The search query / 搜索查询文本。</param>
    /// <param name="topK">Maximum number of results to return / 返回的最大结果数量。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>A list of matching chunk contents / 匹配的文档块内容列表。</returns>
    public async Task<List<string>> SearchAsync(string datasetId, string query, int topK = 5, CancellationToken ct = default)
    {
        // Build a request with Bearer token authentication
        // 使用 Bearer 令牌认证构建请求
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/datasets/{datasetId}/search");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = JsonContent.Create(new { query, top_k = topK });
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var results = new List<string>();
        // Extract content from each chunk in the response
        // 从响应中的每个文档块提取内容
        foreach (var chunk in json.GetProperty("chunks").EnumerateArray())
            results.Add(chunk.GetProperty("content").GetString() ?? "");
        return results;
    }

    /// <summary>
    /// Uploads a document to a dataset using multipart form data.
    /// 使用多部分表单数据将文档上传到数据集。
    /// </summary>
    /// <param name="datasetId">The dataset identifier / 数据集标识符。</param>
    /// <param name="fileName">The file name / 文件名。</param>
    /// <param name="content">The file binary content / 文件二进制内容。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>The document ID assigned by the API / API 返回的文档 ID。</returns>
    public async Task<string> UploadDocumentAsync(string datasetId, string fileName, byte[] content, CancellationToken ct = default)
    {
        // Build multipart form data with the file content
        // 使用文件内容构建多部分表单数据
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(content), "file", fileName);
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/datasets/{datasetId}/documents") { Content = form };
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("document_id").GetString() ?? "";
    }
}
