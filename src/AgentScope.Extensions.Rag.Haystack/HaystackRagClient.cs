using System.Net.Http.Json;
using System.Text.Json;

namespace AgentScope.Extensions.Rag.Haystack;

public sealed class HaystackRagClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public HaystackRagClient(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<List<string>> QueryAsync(string pipelineId, string query, int topK = 5, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/pipelines/{pipelineId}/query", new { query, top_k = topK }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var results = new List<string>();
        foreach (var doc in json.GetProperty("documents").EnumerateArray())
            results.Add(doc.GetProperty("content").GetString() ?? "");
        return results;
    }

    public async Task<string> IndexDocumentAsync(string pipelineId, string text, string? docId = null, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/pipelines/{pipelineId}/documents", new { id = docId ?? Guid.NewGuid().ToString(), content = text }, ct);
        resp.EnsureSuccessStatusCode();
        return docId ?? "";
    }
}
