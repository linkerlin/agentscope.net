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

namespace AgentScope.Extensions.Mem.Mem0;

/// <summary>
/// Mem0 long-term memory client.
/// Mem0 长短期记忆客户端，用于存储和搜索用户与代理相关的记忆。
/// </summary>
public sealed class Mem0LongTermMemory
{
    /// <summary>
    /// HTTP client for API communication.
    /// 用于与 Mem0 API 通信的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _http;

    /// <summary>
    /// API key for authentication.
    /// API 密钥，用于身份认证。
    /// </summary>
    private readonly string _apiKey;

    /// <summary>
    /// Base URL of the Mem0 API.
    /// Mem0 API 的基础地址。
    /// </summary>
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of <see cref="Mem0LongTermMemory"/>.
    /// 初始化 <see cref="Mem0LongTermMemory"/> 类的新实例。
    /// </summary>
    /// <param name="http">The HTTP client instance / HTTP 客户端实例。</param>
    /// <param name="apiKey">The API key / API 密钥。</param>
    /// <param name="baseUrl">Optional custom base URL; defaults to Mem0 API / 可选的自定义基础地址，默认为 Mem0 API。</param>
    public Mem0LongTermMemory(HttpClient http, string apiKey, string? baseUrl = null)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://api.mem0.ai/v1";
    }

    /// <summary>
    /// Adds a memory entry for a specific user and agent.
    /// 添加指定用户和代理的记忆条目。
    /// </summary>
    /// <param name="userId">The user identifier / 用户标识符。</param>
    /// <param name="agentId">The agent identifier / 代理标识符。</param>
    /// <param name="message">The memory message text / 记忆消息文本。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>The memory ID assigned by the API / API 返回的记忆 ID。</returns>
    public async Task<string> AddAsync(string userId, string agentId, string message, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/memories", new
        {
            user_id = userId,
            agent_id = agentId,
            text = message
        }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("id").GetString() ?? "";
    }

    /// <summary>
    /// Searches memories for a specific user and agent.
    /// 搜索指定用户和代理的记忆。
    /// </summary>
    /// <param name="userId">The user identifier / 用户标识符。</param>
    /// <param name="agentId">The agent identifier / 代理标识符。</param>
    /// <param name="query">The search query / 搜索查询文本。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>A list of matching memory texts / 匹配的记忆文本列表。</returns>
    public async Task<List<string>> SearchAsync(string userId, string agentId, string query, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/memories/search", new
        {
            user_id = userId,
            agent_id = agentId,
            query
        }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var results = new List<string>();
        // Iterate through each result item in the response
        // 遍历响应中的每条结果条目
        foreach (var item in json.GetProperty("results").EnumerateArray())
        {
            var text = item.GetProperty("text").GetString();
            if (text != null) results.Add(text);
        }
        return results;
    }
}
