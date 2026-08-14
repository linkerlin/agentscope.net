using System.Net.Http.Json;
using System.Text.Json;

namespace AgentScope.Extensions.Rag.RagFlow;

public sealed class RagFlowRagClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;

    public RagFlowRagClient(HttpClient http, string apiKey, string? baseUrl = null)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl ?? "https://api.ragflow.io/v1";
    }

    public async Task<List<string>> SearchAsync(string datasetId, string query, int topK = 5, CancellationToken ct = default)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/datasets/{datasetId}/search");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = JsonContent.Create(new { query, top_k = topK });
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var results = new List<string>();
        foreach (var chunk in json.GetProperty("chunks").EnumerateArray())
            results.Add(chunk.GetProperty("content").GetString() ?? "");
        return results;
    }

    public async Task<string> UploadDocumentAsync(string datasetId, string fileName, byte[] content, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(content), "file", fileName);
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/datasets/{datasetId}/documents") { Content = form };
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("document_id").GetString() ?? "";
    }
}
