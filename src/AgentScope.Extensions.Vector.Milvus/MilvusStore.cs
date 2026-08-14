using System.Runtime.CompilerServices;
using AgentScope.Extensions.Vector;

namespace AgentScope.Extensions.Vector.Milvus;

/// <summary>
/// Milvus 向量存储适配器（HTTP API 实现）。对标 Java MilvusStore。
/// 通过 Milvus RESTful API 交互（不依赖 gRPC SDK 版本兼容性）。
/// </summary>
public sealed class MilvusStore(HttpClient httpClient, string baseUrl, string collectionName, int dimension) : IVectorStore
{
    public int Dimension => dimension;

    public async ValueTask UpsertAsync(string collection, string id, float[] vector,
        IDictionary<string, object>? payload = null, CancellationToken ct = default)
    {
        var col = collection ?? collectionName;
        var fields = new Dictionary<string, object> { ["id"] = id, ["vector"] = vector };
        if (payload != null)
            foreach (var kv in payload) fields[kv.Key] = kv.Value;

        var body = new { collectionName = col, fieldsData = new[] { fields } };
        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var resp = await httpClient.PostAsync($"{baseUrl}/v1/vector/insert", content, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async IAsyncEnumerable<SearchHit> SearchAsync(string collection, float[] query,
        int topK = 5, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var col = collection ?? collectionName;
        var body = new
        {
            collectionName = col,
            vector = query,
            limit = topK,
            outputFields = new[] { "id" }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var resp = await httpClient.PostAsync($"{baseUrl}/v1/vector/search", content, ct);
        resp.EnsureSuccessStatusCode();

        var resultJson = await resp.Content.ReadAsStringAsync(ct);
        var doc = System.Text.Json.JsonDocument.Parse(resultJson);
        if (doc.RootElement.TryGetProperty("results", out var results))
        {
            foreach (var item in results.EnumerateArray())
            {
                ct.ThrowIfCancellationRequested();
                var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                var score = item.TryGetProperty("distance", out var dEl) ? (float)dEl.GetDouble() : 0;
                yield return new SearchHit(id, score);
            }
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
