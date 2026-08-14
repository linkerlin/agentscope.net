// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Linq;
using System.Net.Http.Json;
using AgentScope.Core.RAG;

namespace AgentScope.Extensions.Embedding;

/// <summary>
/// Ollama 文本向量生成器。对标 Java OllamaTextEmbedding。
/// 默认连接本地 Ollama 服务 http://localhost:11434，无需 API Key。
/// 复用 Core IEmbeddingGenerator 接口。
/// </summary>
public sealed class OllamaEmbeddingGenerator(
    HttpClient httpClient,
    string model = "nomic-embed-text",
    int dimension = 768) : IEmbeddingGenerator
{
    private const string EmbedEndpoint = "http://localhost:11434/api/embeddings";
    private const string EmbedBatchEndpoint = "http://localhost:11434/api/embed";

    public int EmbeddingDimension => dimension;

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, EmbedEndpoint)
        {
            Content = JsonContent.Create(new EmbedRequest(model, text))
        };
        using var res = await httpClient.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<EmbedResponse>(ct);
        return dto?.Embedding ?? throw new InvalidOperationException("Ollama 返回空向量");
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts, CancellationToken ct = default)
    {
        var list = texts.ToList();
        using var req = new HttpRequestMessage(HttpMethod.Post, EmbedBatchEndpoint)
        {
            Content = JsonContent.Create(new EmbedBatchRequest(model, list))
        };
        using var res = await httpClient.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<EmbedBatchResponse>(ct);
        return dto?.Embeddings ?? [];
    }

    private sealed record EmbedRequest(string Model, string Prompt);
    private sealed record EmbedResponse(float[] Embedding);
    private sealed record EmbedBatchRequest(string Model, List<string> Input);
    private sealed record EmbedBatchResponse(List<float[]> Embeddings);
}
