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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Core.RAG;

/// <summary>
/// In-memory vector store implementation.
/// 内存向量存储实现
/// 
/// Java参考: io.agentscope.core.rag.VectorStore
/// </summary>
public class InMemoryVectorStore : IKnowledge
{
    private readonly Dictionary<string, KnowledgeDocument> _documents = new();
    private readonly IEmbeddingGenerator? _embeddingGenerator;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public InMemoryVectorStore(IEmbeddingGenerator? embeddingGenerator = null)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    /// <inheritdoc />
    public async Task<string> AddDocumentAsync(KnowledgeDocument document, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Generate embedding if not provided and generator is available
            if (document.Embedding == null && _embeddingGenerator != null && !string.IsNullOrEmpty(document.Content))
            {
                document.Embedding = await _embeddingGenerator.GenerateEmbeddingAsync(document.Content, cancellationToken);
            }

            _documents[document.Id] = document;
            return document.Id;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> AddDocumentsAsync(IEnumerable<KnowledgeDocument> documents, CancellationToken cancellationToken = default)
    {
        var ids = new List<string>();
        
        foreach (var doc in documents)
        {
            var id = await AddDocumentAsync(doc, cancellationToken);
            ids.Add(id);
        }

        return ids;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(string query, KnowledgeSearchOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_embeddingGenerator == null)
        {
            throw new InvalidOperationException("Embedding generator is required for search");
        }

        var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(query, cancellationToken);
        return await SearchByEmbeddingAsync(queryEmbedding, options, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<KnowledgeSearchResult>> SearchByEmbeddingAsync(float[] embedding, KnowledgeSearchOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new KnowledgeSearchOptions();
        
        return _lock.WaitAsync(cancellationToken).ContinueWith(_ =>
        {
            try
            {
                var results = new List<KnowledgeSearchResult>();

                foreach (var doc in _documents.Values)
                {
                    // Apply metadata filters
                    if (options.Filters != null && !MatchesFilters(doc, options.Filters))
                    {
                        continue;
                    }

                    // Calculate similarity if document has embedding
                    float score = 0;
                    if (doc.Embedding != null && embedding.Length == doc.Embedding.Length)
                    {
                        score = CosineSimilarity(embedding, doc.Embedding);
                    }

                    // Apply minimum score filter
                    if (options.MinScore.HasValue && score < options.MinScore.Value)
                    {
                        continue;
                    }

                    var result = new KnowledgeSearchResult
                    {
                        Document = options.IncludeEmbeddings ? doc : CloneWithoutEmbedding(doc),
                        Score = score
                    };
                    results.Add(result);
                }

                // Sort by score descending and take top-k
                var sorted = results
                    .OrderByDescending(r => r.Score)
                    .Take(options.TopK)
                    .ToList();

                // Set ranks
                for (int i = 0; i < sorted.Count; i++)
                {
                    sorted[i].Rank = i + 1;
                }

                return (IReadOnlyList<KnowledgeSearchResult>)sorted;
            }
            finally
            {
                _lock.Release();
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        return _lock.WaitAsync(cancellationToken).ContinueWith(_ =>
        {
            try
            {
                return _documents.Remove(documentId);
            }
            finally
            {
                _lock.Release();
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> DeleteDocumentsAsync(Dictionary<string, object> filters, CancellationToken cancellationToken = default)
    {
        return _lock.WaitAsync(cancellationToken).ContinueWith(_ =>
        {
            try
            {
                var toDelete = _documents.Values.Where(doc => MatchesFilters(doc, filters)).Select(doc => doc.Id).ToList();
                foreach (var id in toDelete)
                {
                    _documents.Remove(id);
                }
                return toDelete.Count;
            }
            finally
            {
                _lock.Release();
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<KnowledgeDocument?> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        return _lock.WaitAsync(cancellationToken).ContinueWith(_ =>
        {
            try
            {
                _documents.TryGetValue(documentId, out var doc);
                return doc != null ? CloneDocument(doc) : null;
            }
            finally
            {
                _lock.Release();
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateDocumentAsync(KnowledgeDocument document, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_documents.ContainsKey(document.Id))
            {
                return false;
            }

            // Regenerate embedding if content changed
            if (_embeddingGenerator != null && !string.IsNullOrEmpty(document.Content))
            {
                document.Embedding = await _embeddingGenerator.GenerateEmbeddingAsync(document.Content, cancellationToken);
            }

            _documents[document.Id] = document;
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        return _lock.WaitAsync(cancellationToken).ContinueWith(_ =>
        {
            try
            {
                return _documents.Count;
            }
            finally
            {
                _lock.Release();
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        return _lock.WaitAsync(cancellationToken).ContinueWith(_ =>
        {
            try
            {
                _documents.Clear();
            }
            finally
            {
                _lock.Release();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Calculates cosine similarity between two vectors.
    /// </summary>
    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Vectors must have same length");
        }

        float dotProduct = 0;
        float normA = 0;
        float normB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
        {
            return 0;
        }

        return dotProduct / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    /// <summary>
    /// Checks if document matches metadata filters.
    /// </summary>
    private static bool MatchesFilters(KnowledgeDocument doc, Dictionary<string, object> filters)
    {
        foreach (var filter in filters)
        {
            if (!doc.Metadata.TryGetValue(filter.Key, out var value))
            {
                return false;
            }

            if (!Equals(value, filter.Value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Clones a document without embedding.
    /// </summary>
    private static KnowledgeDocument CloneWithoutEmbedding(KnowledgeDocument doc)
    {
        return new KnowledgeDocument
        {
            Id = doc.Id,
            Title = doc.Title,
            Content = doc.Content,
            Source = doc.Source,
            Metadata = new Dictionary<string, object>(doc.Metadata),
            CreatedAt = doc.CreatedAt,
            ChunkIndex = doc.ChunkIndex,
            TotalChunks = doc.TotalChunks
            // Embedding is not copied
        };
    }

    /// <summary>
    /// Clones a document.
    /// </summary>
    private static KnowledgeDocument CloneDocument(KnowledgeDocument doc)
    {
        var clone = CloneWithoutEmbedding(doc);
        clone.Embedding = doc.Embedding?.ToArray();
        return clone;
    }
}

/// <summary>
/// 确定性伪随机 embedding 生成器，仅供单元测试 / 演示使用。
/// 它不提供任何语义能力，仅保证同一文本产生稳定向量。
/// 生产环境请使用 <see cref="OpenAIEmbeddingGenerator"/> 等真实 provider。
/// 原 SimpleEmbeddingGenerator 已重命名，保留此别名以兼容旧调用。
/// </summary>
[Obsolete("Use FakeEmbeddingGenerator for tests, or a real provider such as OpenAIEmbeddingGenerator.")]
public class SimpleEmbeddingGenerator : FakeEmbeddingGenerator
{
    public SimpleEmbeddingGenerator(int dimension = 1536) : base(dimension) { }
}

/// <summary>
/// 确定性伪随机 embedding 生成器（测试 / 占位用）。
/// 不提供语义能力；真实检索请使用 OpenAIEmbeddingGenerator 等实现 IEmbeddingGenerator 的 provider。
/// </summary>
public class FakeEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly int _dimension;

    public FakeEmbeddingGenerator(int dimension = 1536)
    {
        _dimension = dimension;
    }

    public int EmbeddingDimension => _dimension;

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        // 基于文本哈希的确定性伪随机向量（无语义，仅测试用）
        var hash = text.GetHashCode();
        var embedding = new float[_dimension];
        var rng = new Random(hash);

        for (int i = 0; i < _dimension; i++)
        {
            embedding[i] = (float)(rng.NextDouble() * 2 - 1); // Range: [-1, 1]
        }

        // 归一化
        var norm = (float)Math.Sqrt(embedding.Sum(x => x * x));
        if (norm > 0)
        {
            for (int i = 0; i < _dimension; i++)
            {
                embedding[i] /= norm;
            }
        }

        return Task.FromResult(embedding);
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            results.Add(await GenerateEmbeddingAsync(text, cancellationToken));
        }
        return results;
    }
}

/// <summary>
/// 真实 embedding provider：调用 OpenAI embeddings API。
/// 对应 Java: io.agentscope.core.rag.model / extensions 中的 embedding 实现。
/// 这里给出可编译的结构与 HTTP 调用骨架；正式接入时复用 Model/Transport 的 IHttpTransport。
/// </summary>
public class OpenAIEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _http;

    public int EmbeddingDimension { get; }

    public OpenAIEmbeddingGenerator(string apiKey, string model = "text-embedding-3-small", int dimension = 1536, HttpClient? http = null)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _model = model;
        EmbeddingDimension = dimension;
        _http = http ?? new HttpClient();
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings")
        {
            Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { input = text, model = _model }),
                System.Text.Encoding.UTF8,
                "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");

        var vec = new float[data.GetArrayLength()];
        var i = 0;
        foreach (var item in data.EnumerateArray())
        {
            vec[i++] = item.GetSingle();
        }

        return vec;
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            results.Add(await GenerateEmbeddingAsync(text, cancellationToken));
        }
        return results;
    }
}
