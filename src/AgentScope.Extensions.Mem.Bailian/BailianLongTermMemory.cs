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

namespace AgentScope.Extensions.Mem.Bailian;

/// <summary>
/// Bailian (Alibaba Cloud) long-term memory client.
/// 阿里云百炼平台的长短期记忆客户端，用于存储和检索会话记忆。
/// </summary>
public sealed class BailianLongTermMemory
{
    /// <summary>
    /// HTTP client for API communication.
    /// 用于与百炼 API 通信的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _http;

    /// <summary>
    /// API key for authentication.
    /// API 密钥，用于身份认证。
    /// </summary>
    private readonly string _apiKey;

    /// <summary>
    /// Base URL of the Bailian API.
    /// 百炼 API 的基础地址。
    /// </summary>
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of <see cref="BailianLongTermMemory"/>.
    /// 初始化 <see cref="BailianLongTermMemory"/> 类的新实例。
    /// </summary>
    /// <param name="http">The HTTP client instance / HTTP 客户端实例。</param>
    /// <param name="apiKey">The API key / API 密钥。</param>
    /// <param name="baseUrl">Optional custom base URL; defaults to Bailian API / 可选的自定义基础地址，默认为百炼 API。</param>
    public BailianLongTermMemory(HttpClient http, string apiKey, string? baseUrl = null)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://bailian.aliyuncs.com/api/v1";
    }

    /// <summary>
    /// Stores memory content for a session.
    /// 存储指定会话的记忆内容。
    /// </summary>
    /// <param name="sessionId">The session identifier / 会话标识符。</param>
    /// <param name="content">The memory content to store / 要存储的记忆内容。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>The memory ID assigned by the API / API 返回的记忆 ID。</returns>
    public async Task<string> StoreAsync(string sessionId, string content, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/memory/store", new
        {
            session_id = sessionId,
            content
        }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("memory_id").GetString() ?? "";
    }

    /// <summary>
    /// Retrieves relevant memories for a session based on a query.
    /// 根据查询检索与指定会话相关的记忆。
    /// </summary>
    /// <param name="sessionId">The session identifier / 会话标识符。</param>
    /// <param name="query">The search query / 搜索查询文本。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>A list of matching memory texts / 匹配的记忆文本列表。</returns>
    public async Task<List<string>> RetrieveAsync(string sessionId, string query, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/memory/retrieve", new
        {
            session_id = sessionId,
            query
        }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var results = new List<string>();
        // Iterate through each memory item in the response array
        // 遍历响应数组中的每条记忆条目
        foreach (var item in json.GetProperty("memories").EnumerateArray())
        {
            var text = item.GetProperty("content").GetString();
            if (text != null) results.Add(text);
        }
        return results;
    }
}
