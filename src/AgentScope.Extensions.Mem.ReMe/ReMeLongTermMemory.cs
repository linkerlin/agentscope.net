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

namespace AgentScope.Extensions.Mem.ReMe;

/// <summary>
/// ReMe long-term memory client.
/// ReMe 长短期记忆客户端，用于保存和查询用户记忆。
/// </summary>
public sealed class ReMeLongTermMemory
{
    /// <summary>
    /// HTTP client for API communication.
    /// 用于与 ReMe API 通信的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _http;

    /// <summary>
    /// Base URL of the ReMe API.
    /// ReMe API 的基础地址。
    /// </summary>
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of <see cref="ReMeLongTermMemory"/>.
    /// 初始化 <see cref="ReMeLongTermMemory"/> 类的新实例。
    /// </summary>
    /// <param name="http">The HTTP client instance / HTTP 客户端实例。</param>
    /// <param name="baseUrl">Optional custom base URL; defaults to ReMe API / 可选的自定义基础地址，默认为 ReMe API。</param>
    public ReMeLongTermMemory(HttpClient http, string? baseUrl = null)
    {
        _http = http;
        _baseUrl = baseUrl ?? "https://api.reme.ai/v1";
    }

    /// <summary>
    /// Saves a memory text for a specific user.
    /// 保存指定用户的记忆文本。
    /// </summary>
    /// <param name="userId">The user identifier / 用户标识符。</param>
    /// <param name="memoryText">The memory text to save / 要保存的记忆文本。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>The memory ID assigned by the API / API 返回的记忆 ID。</returns>
    public async Task<string> SaveAsync(string userId, string memoryText, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/memories", new
        {
            user_id = userId,
            text = memoryText
        }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("id").GetString() ?? "";
    }

    /// <summary>
    /// Queries memories for a specific user.
    /// 查询指定用户的记忆。
    /// </summary>
    /// <param name="userId">The user identifier / 用户标识符。</param>
    /// <param name="query">The search query / 搜索查询文本。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>A list of matching memory texts / 匹配的记忆文本列表。</returns>
    public async Task<List<string>> QueryAsync(string userId, string query, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/memories/query", new
        {
            user_id = userId,
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
