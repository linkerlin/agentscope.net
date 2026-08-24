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

namespace AgentScope.Extensions.Higress;

public sealed class HigressMcpClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public HigressMcpClient(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<JsonElement> CallToolAsync(string toolName, JsonElement args, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/mcp/tools/{toolName}", new { arguments = args }, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
    }

    public async Task<List<string>> ListToolsAsync(CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/mcp/tools", ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var tools = new List<string>();
        foreach (var t in json.GetProperty("tools").EnumerateArray())
            tools.Add(t.GetProperty("name").GetString() ?? "");
        return tools;
    }
}
