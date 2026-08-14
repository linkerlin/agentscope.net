using System.Net.Http.Json;
using System.Text.Json;

namespace AgentScope.Extensions.Mem.Bailian;

public sealed class BailianLongTermMemory
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public BailianLongTermMemory(HttpClient http, string apiKey, string? baseUrl = null)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://bailian.aliyuncs.com/api/v1";
    }

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
        foreach (var item in json.GetProperty("memories").EnumerateArray())
        {
            var text = item.GetProperty("content").GetString();
            if (text != null) results.Add(text);
        }
        return results;
    }
}
