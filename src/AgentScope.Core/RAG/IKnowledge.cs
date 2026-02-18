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
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Core.RAG;

/// <summary>
/// Knowledge document model.
/// 知识文档模型
/// </summary>
public class KnowledgeDocument
{
    /// <summary>
    /// Unique document ID.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Document title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Document content.
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// Document source/URL.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Document metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Embedding vector.
    /// </summary>
    public float[]? Embedding { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Chunk index if document is split.
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Total chunks if document is split.
    /// </summary>
    public int TotalChunks { get; set; } = 1;
}

/// <summary>
/// Search result from knowledge base.
/// 知识库搜索结果
/// </summary>
public class KnowledgeSearchResult
{
    /// <summary>
    /// The matched document.
    /// </summary>
    public KnowledgeDocument Document { get; set; } = new();

    /// <summary>
    /// Similarity score (0-1).
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Rank in search results.
    /// </summary>
    public int Rank { get; set; }
}

/// <summary>
/// Search options for knowledge retrieval.
/// 知识检索选项
/// </summary>
public class KnowledgeSearchOptions
{
    /// <summary>
    /// Maximum number of results.
    /// </summary>
    public int TopK { get; set; } = 5;

    /// <summary>
    /// Minimum similarity threshold (0-1).
    /// </summary>
    public float? MinScore { get; set; }

    /// <summary>
    /// Filter by metadata.
    /// </summary>
    public Dictionary<string, object>? Filters { get; set; }

    /// <summary>
    /// Whether to include embeddings in results.
    /// </summary>
    public bool IncludeEmbeddings { get; set; } = false;
}

/// <summary>
/// Interface for knowledge base operations.
/// 知识库接口
/// 
/// Java参考: io.agentscope.core.rag.Knowledge
/// </summary>
public interface IKnowledge
{
    /// <summary>
    /// Adds a document to the knowledge base.
    /// </summary>
    Task<string> AddDocumentAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple documents in batch.
    /// </summary>
    Task<IReadOnlyList<string>> AddDocumentsAsync(IEnumerable<KnowledgeDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for relevant documents.
    /// </summary>
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(string query, KnowledgeSearchOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches using an embedding vector.
    /// </summary>
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchByEmbeddingAsync(float[] embedding, KnowledgeSearchOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document by ID.
    /// </summary>
    Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes documents by filter.
    /// </summary>
    Task<int> DeleteDocumentsAsync(Dictionary<string, object> filters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by ID.
    /// </summary>
    Task<KnowledgeDocument?> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a document.
    /// </summary>
    Task<bool> UpdateDocumentAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total document count.
    /// </summary>
    Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all documents.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for embedding generation.
/// 嵌入生成接口
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>
    /// Generates embedding for text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for multiple texts.
    /// </summary>
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Embedding dimension size.
    /// </summary>
    int EmbeddingDimension { get; }
}

/// <summary>
/// RAG mode enumeration.
/// RAG 模式枚举
/// </summary>
public enum RAGMode
{
    /// <summary>
    /// Retrieve and add to context.
    /// 检索并添加到上下文
    /// </summary>
    Retrieval,

    /// <summary>
    /// Retrieve and generate answer.
    /// 检索并生成答案
    /// </summary>
    RetrievalQA,

    /// <summary>
    /// Retrieve only.
    /// 仅检索
    /// </summary>
    RetrievalOnly
}

/// <summary>
/// Knowledge base configuration.
/// 知识库配置
/// </summary>
public class KnowledgeConfig
{
    /// <summary>
    /// Knowledge base name.
    /// </summary>
    public string Name { get; set; } = "default";

    /// <summary>
    /// Embedding dimension.
    /// </summary>
    public int EmbeddingDimension { get; set; } = 1536;

    /// <summary>
    /// Default top-k for retrieval.
    /// </summary>
    public int DefaultTopK { get; set; } = 5;

    /// <summary>
    /// Default similarity threshold.
    /// </summary>
    public float? DefaultMinScore { get; set; }

    /// <summary>
    /// Chunk size for document splitting.
    /// </summary>
    public int ChunkSize { get; set; } = 1000;

    /// <summary>
    /// Chunk overlap size.
    /// </summary>
    public int ChunkOverlap { get; set; } = 200;

    /// <summary>
    /// Whether to enable hybrid search (keyword + semantic).
    /// </summary>
    public bool EnableHybridSearch { get; set; } = true;
}
