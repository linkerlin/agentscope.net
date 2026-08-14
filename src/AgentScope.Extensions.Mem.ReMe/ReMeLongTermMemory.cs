using System.Net.Http.Json;
using System.Text.Json;

namespace AgentScope.Extensions.Mem.ReMe;

public sealed class ReMeLongTermMemory
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public ReMeLongTermMemory(HttpClient http, string? baseUrl = null)
    {
        _http = http;
        _baseUrl = baseUrl ?? "https://api.reme.ai/v1";
    }

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
        foreach (var item in json.GetProperty("results").EnumerateArray())
        {
            var text = item.GetProperty("text").GetString();
            if (text != null) results.Add(text);
        }
        return results;
    }
}
