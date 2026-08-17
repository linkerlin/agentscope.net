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

namespace AgentScope.Extensions.Studio;

public sealed class AgentScopeStudioClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public AgentScopeStudioClient(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<string> CreateSessionAsync(string agentId, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/api/sessions", new { agent_id = agentId }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("session_id").GetString() ?? "";
    }

    public async Task LogEventAsync(string sessionId, string type, string data, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/api/sessions/{sessionId}/events", new { type, data }, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<JsonElement> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/api/sessions/{sessionId}", ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
    }
}
