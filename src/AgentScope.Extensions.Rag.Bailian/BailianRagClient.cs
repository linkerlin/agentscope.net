using System.Net.Http.Json;
using System.Text.Json;

namespace AgentScope.Extensions.Rag.Bailian;

public sealed class BailianRagClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public BailianRagClient(HttpClient http, string apiKey, string? baseUrl = null)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://bailian.aliyuncs.com/api/v1/rag";
    }

    public async Task<List<string>> SearchAsync(string indexId, string query, int topK = 5, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/indexes/{indexId}/search", new { query, top_k = topK }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var results = new List<string>();
        foreach (var item in json.GetProperty("results").EnumerateArray())
            results.Add(item.GetProperty("text").GetString() ?? "");
        return results;
    }

    public async Task<string> CreateIndexAsync(string name, string description, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/indexes", new { name, description }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("index_id").GetString() ?? "";
    }
}
