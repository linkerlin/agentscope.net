using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AgentScope.Core.RAG;

namespace AgentScope.Extensions.Embedding;

/// <summary>
/// OpenAI 文本向量生成器。对标 Java OpenAITextEmbedding。
/// 复用 Core IEmbeddingGenerator 接口。
/// </summary>
public sealed class OpenAIEmbeddingGenerator(
    HttpClient httpClient,
    string model = "text-embedding-3-small",
    int dimension = 1536) : IEmbeddingGenerator
{
    private const string Endpoint = "https://api.openai.com/v1/embeddings";
    private readonly string _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? throw new InvalidOperationException("缺少 OPENAI_API_KEY");

    public int EmbeddingDimension => dimension;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new EmbeddingRequest(model, [text]))
        };
        req.Headers.Authorization = new("Bearer", _apiKey);
        using var res = await httpClient.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
        return dto?.Data[0].Embedding ?? throw new InvalidOperationException("OpenAI 返回空向量");
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts, CancellationToken ct = default)
    {
        var list = texts.ToList();
        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new EmbeddingRequest(model, list.ToArray()))
        };
        req.Headers.Authorization = new("Bearer", _apiKey);
        using var res = await httpClient.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
        return dto?.Data.Select(x => x.Embedding).ToArray() ?? [];
    }

    private sealed record EmbeddingRequest(string Model, string[] Input);
    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")] EmbeddingData[] Data);
    private sealed record EmbeddingData(
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
