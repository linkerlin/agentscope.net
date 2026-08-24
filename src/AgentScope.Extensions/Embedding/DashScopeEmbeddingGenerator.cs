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
using System.Text.Json.Serialization;
using AgentScope.Core.RAG;

namespace AgentScope.Extensions.Embedding;

/// <summary>
/// DashScope text embedding generator (OpenAI-compatible mode). Maps to Java DashScopeTextEmbedding.
/// Reuses the Core IEmbeddingGenerator interface. Reads the DASHSCOPE_API_KEY environment variable by default.
/// DashScope 文本向量生成器（OpenAI 兼容模式）。对标 Java DashScopeTextEmbedding。
/// 复用 Core IEmbeddingGenerator 接口，默认读取 DASHSCOPE_API_KEY 环境变量。
/// </summary>
public sealed class DashScopeEmbeddingGenerator(
    HttpClient httpClient,
    string model = "text-embedding-v3",
    int dimension = 1536) : IEmbeddingGenerator
{
    private const string Endpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1/embeddings";

    /// <summary>API key read from the DASHSCOPE_API_KEY environment variable. 从 DASHSCOPE_API_KEY 环境变量读取的 API 密钥。</summary>
    private readonly string _apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY")
        ?? throw new InvalidOperationException("缺少 DASHSCOPE_API_KEY 环境变量");

    /// <inheritdoc />
    public int EmbeddingDimension => dimension;

    /// <inheritdoc />
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        // Build the HTTP request for a single text embedding
        // 构建单文本向量的 HTTP 请求
        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new EmbeddingRequest(model, [text]))
        };
        req.Headers.Authorization = new("Bearer", _apiKey);
        using var res = await httpClient.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
        return dto?.Data[0].Embedding ?? throw new InvalidOperationException("DashScope 返回空向量");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts, CancellationToken ct = default)
    {
        // Build the HTTP request for batch text embedding
        // 构建批量文本向量的 HTTP 请求
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

    /// <summary>Request DTO for the DashScope embeddings API. DashScope 向量 API 的请求 DTO。</summary>
    private sealed record EmbeddingRequest(string Model, string[] Input);

    /// <summary>Response DTO from the DashScope embeddings API. DashScope 向量 API 的响应 DTO。</summary>
    private sealed record EmbeddingResponse([property: JsonPropertyName("data")] EmbeddingData[] Data);

    /// <summary>Individual embedding data item in the response. 响应中的单个向量数据项。</summary>
    private sealed record EmbeddingData([property: JsonPropertyName("embedding")] float[] Embedding);
}
