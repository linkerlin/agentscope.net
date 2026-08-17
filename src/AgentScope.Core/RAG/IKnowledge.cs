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
/// Represents a knowledge document with content, metadata, and optional embedding vector.
/// 表示包含内容、元数据和可选嵌入向量的知识文档。
/// </summary>
public class KnowledgeDocument
{
    /// <summary>
    /// Unique document ID. / 唯一文档标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Document title. / 文档标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Document content. / 文档内容
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// Document source URL or origin. / 文档来源/URL
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Document metadata key-value pairs. / 文档元数据键值对
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Embedding vector for semantic search. / 语义搜索用的嵌入向量
    /// </summary>
    public float[]? Embedding { get; set; }

    /// <summary>
    /// Creation timestamp (UTC). / 创建时间戳（UTC）
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Chunk index when the document is split into multiple chunks. / 文档分片时的分片索引
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Total number of chunks when the document is split. / 文档分片时的总分片数
    /// </summary>
    public int TotalChunks { get; set; } = 1;
}

/// <summary>
/// Represents a search result from the knowledge base, containing the matched document and relevance score.
/// 表示知识库的搜索结果，包含匹配的文档和相关性得分。
/// </summary>
public class KnowledgeSearchResult
{
    /// <summary>
    /// The matched document. / 匹配的文档
    /// </summary>
    public KnowledgeDocument Document { get; set; } = new();

    /// <summary>
    /// Similarity score ranging from 0 to 1. / 相似度得分，范围 0-1
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// Rank position in the search results. / 在搜索结果中的排名
    /// </summary>
    public int Rank { get; set; }
}

/// <summary>
/// Search options for knowledge retrieval, controlling result count, scoring threshold, and filtering.
/// 知识检索选项，控制结果数量、评分阈值和过滤条件。
/// </summary>
public class KnowledgeSearchOptions
{
    /// <summary>
    /// Maximum number of results to return. / 返回结果的最大数量
    /// </summary>
    public int TopK { get; set; } = 5;

    /// <summary>
    /// Minimum similarity threshold (0-1). Results below this score are excluded. / 最小相似度阈值（0-1），低于此分数的结果被排除
    /// </summary>
    public float? MinScore { get; set; }

    /// <summary>
    /// Metadata filters to narrow search results. / 用于缩小搜索结果的元数据过滤器
    /// </summary>
    public Dictionary<string, object>? Filters { get; set; }

    /// <summary>
    /// Whether to include embedding vectors in the search results. / 是否在搜索结果中包含嵌入向量
    /// </summary>
    public bool IncludeEmbeddings { get; set; } = false;
}

/// <summary>
/// Interface for knowledge base operations, defining document management and semantic search capabilities.
/// 知识库操作接口，定义文档管理和语义搜索功能。
///
/// Java reference: io.agentscope.core.rag.Knowledge
/// </summary>
public interface IKnowledge
{
    /// <summary>
    /// Adds a document to the knowledge base and returns its generated ID.
    /// 向知识库添加文档，返回生成的文档ID。
    /// </summary>
    Task<string> AddDocumentAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple documents in batch and returns their generated IDs.
    /// 批量添加多个文档，返回生成的文档ID列表。
    /// </summary>
    Task<IReadOnlyList<string>> AddDocumentsAsync(IEnumerable<KnowledgeDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for documents relevant to the given text query.
    /// 搜索与给定文本查询相关的文档。
    /// </summary>
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(string query, KnowledgeSearchOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for documents using a pre-computed embedding vector.
    /// 使用预计算的嵌入向量搜索相关文档。
    /// </summary>
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchByEmbeddingAsync(float[] embedding, KnowledgeSearchOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document by its unique ID. Returns true if successful.
    /// 根据唯一ID删除文档。成功返回 true。
    /// </summary>
    Task<bool> DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all documents matching the given metadata filters. Returns the count of deleted documents.
    /// 删除匹配给定元数据过滤器的所有文档。返回删除的文档数量。
    /// </summary>
    Task<int> DeleteDocumentsAsync(Dictionary<string, object> filters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by its unique ID. Returns null if not found.
    /// 根据唯一ID获取文档。未找到时返回 null。
    /// </summary>
    Task<KnowledgeDocument?> GetDocumentAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document. Returns true if the update was successful.
    /// 更新已有文档。更新成功返回 true。
    /// </summary>
    Task<bool> UpdateDocumentAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of documents in the knowledge base.
    /// 获取知识库中的文档总数。
    /// </summary>
    Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all documents from the knowledge base.
    /// 清空知识库中的所有文档。
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for generating text embeddings used in semantic search.
/// 文本嵌入生成接口，用于语义搜索。
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// 为给定文本生成嵌入向量。
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embedding vectors for multiple texts in batch.
    /// 批量生成多个文本的嵌入向量。
    /// </summary>
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the dimension size of generated embedding vectors. / 获取生成的嵌入向量的维度大小
    /// </summary>
    int EmbeddingDimension { get; }
}

/// <summary>
/// Defines the operational mode for RAG (Retrieval-Augmented Generation).
/// RAG（检索增强生成）操作模式枚举。
/// </summary>
public enum RAGMode
{
    /// <summary>
    /// Retrieve relevant context and append it to the original message.
    /// 检索相关上下文并附加到原始消息中。
    /// </summary>
    Retrieval,

    /// <summary>
    /// Retrieve context and use it to generate an answer with the LLM.
    /// 检索上下文并使用 LLM 基于上下文生成答案。
    /// </summary>
    RetrievalQA,

    /// <summary>
    /// Retrieve context only without modifying the message.
    /// 仅检索上下文，不修改原始消息。
    /// </summary>
    RetrievalOnly
}

/// <summary>
/// Configuration settings for a knowledge base, including embedding, chunking, and search parameters.
/// 知识库配置，包含嵌入、分块和搜索参数设置。
/// </summary>
public class KnowledgeConfig
{
    /// <summary>
    /// Knowledge base name. / 知识库名称
    /// </summary>
    public string Name { get; set; } = "default";

    /// <summary>
    /// Embedding vector dimension. / 嵌入向量维度
    /// </summary>
    public int EmbeddingDimension { get; set; } = 1536;

    /// <summary>
    /// Default number of top results to retrieve. / 默认检索结果数量
    /// </summary>
    public int DefaultTopK { get; set; } = 5;

    /// <summary>
    /// Default minimum similarity score threshold. / 默认最小相似度阈值
    /// </summary>
    public float? DefaultMinScore { get; set; }

    /// <summary>
    /// Character chunk size for document splitting. / 文档分块时的字符块大小
    /// </summary>
    public int ChunkSize { get; set; } = 1000;

    /// <summary>
    /// Overlap character count between adjacent chunks. / 相邻块之间的重叠字符数
    /// </summary>
    public int ChunkOverlap { get; set; } = 200;

    /// <summary>
    /// Whether to enable hybrid search combining keyword and semantic matching. / 是否启用结合关键词和语义匹配的混合搜索
    /// </summary>
    public bool EnableHybridSearch { get; set; } = true;
}
