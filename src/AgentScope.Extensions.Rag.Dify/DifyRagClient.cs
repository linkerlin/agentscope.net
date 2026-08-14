using System.Net.Http.Json;
using System.Text.Json;

namespace AgentScope.Extensions.Rag.Dify;

public sealed class DifyRagClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public DifyRagClient(HttpClient http, string apiKey, string? baseUrl = null)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://api.dify.ai/v1";
    }

    public async Task<List<string>> RetrieveAsync(string datasetId, string query, int topK = 5, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/datasets/{datasetId}/retrieve");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = JsonContent.Create(new { query, top_k = topK });
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var results = new List<string>();
        foreach (var doc in json.GetProperty("documents").EnumerateArray())
            results.Add(doc.GetProperty("text").GetString() ?? "");
        return results;
    }
}
