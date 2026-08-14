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
