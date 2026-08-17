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

namespace AgentScope.Extensions.Aistio;

public sealed class AistioControlPlaneClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public AistioControlPlaneClient(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task RegisterAgentAsync(string agentId, string endpoint, AgentCapabilities caps, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/v1/agents", new
        {
            agent_id = agentId,
            endpoint,
            capabilities = caps
        }, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task HeartbeatAsync(string agentId, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"{_baseUrl}/v1/agents/{agentId}/heartbeat", null, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<string>> DiscoverAgentsAsync(string? labelSelector = null, CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/v1/agents";
        if (!string.IsNullOrEmpty(labelSelector)) url += $"?label={labelSelector}";
        var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var agents = new List<string>();
        foreach (var a in json.GetProperty("agents").EnumerateArray())
            agents.Add(a.GetProperty("agent_id").GetString() ?? "");
        return agents;
    }

    public async Task DeregisterAgentAsync(string agentId, CancellationToken ct = default)
    {
        var resp = await _http.DeleteAsync($"{_baseUrl}/v1/agents/{agentId}", ct);
        resp.EnsureSuccessStatusCode();
    }
}

public sealed record AgentCapabilities(string Runtime = "dotnet", int ContractLevel = 3, string Version = "1.2.0");
