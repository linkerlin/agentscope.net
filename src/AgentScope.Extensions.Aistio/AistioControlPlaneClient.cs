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
