using System.Net.Http.Json;
using System.Text.Json;

namespace AgentScope.Extensions.Mem.Mem0;

public sealed class Mem0LongTermMemory
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public Mem0LongTermMemory(HttpClient http, string apiKey, string? baseUrl = null)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://api.mem0.ai/v1";
    }

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
        foreach (var item in json.GetProperty("results").EnumerateArray())
        {
            var text = item.GetProperty("text").GetString();
            if (text != null) results.Add(text);
        }
        return results;
    }
}
