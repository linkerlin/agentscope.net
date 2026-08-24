# RAG 概览

## Core RAG 体系

位于 `AgentScope.Core.RAG`：

### IKnowledge 接口

```csharp
public interface IKnowledge
{
    Task<string> AddDocumentAsync(KnowledgeDocument document, CancellationToken ct = default);
    Task<IReadOnlyList<string>> AddDocumentsAsync(IEnumerable<KnowledgeDocument> documents, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(string query, KnowledgeSearchOptions? options = null, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeSearchResult>> SearchByEmbeddingAsync(float[] embedding, KnowledgeSearchOptions? options = null, CancellationToken ct = default);
    Task<bool> DeleteDocumentAsync(string documentId, CancellationToken ct = default);
    Task<int> DeleteDocumentsAsync(Dictionary<string, object> filters, CancellationToken ct = default);
    Task<KnowledgeDocument?> GetDocumentAsync(string documentId, CancellationToken ct = default);
    Task<bool> UpdateDocumentAsync(KnowledgeDocument document, CancellationToken ct = default);
    Task<int> GetDocumentCountAsync(CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}
```

### InMemoryVectorStore

`InMemoryVectorStore(IEmbeddingGenerator? embeddingGenerator = null)` 实现 `IKnowledge`。注入 `IEmbeddingGenerator` 后可做向量语义检索。

### VectorStore 扩展

`AgentScope.Extensions.Vector.IVectorStore` 定义：

```csharp
public interface IVectorStore : IAsyncDisposable
{
    int Dimension { get; }
    ValueTask UpsertAsync(string collection, string id, float[] vector,
        IDictionary<string, object>? payload = null, CancellationToken ct = default);
    IAsyncEnumerable<SearchHit> SearchAsync(string collection, float[] query,
        int topK = 5, CancellationToken ct = default);
}
```

| 实现 | 构造函数 |
| --- | --- |
| `ElasticsearchStore` | `(ElasticsearchClient client, string indexName, int dimension)` |
| `MilvusStore` | `(HttpClient httpClient, string baseUrl, string collectionName, int dimension)` |
| `PgVectorStore` | `(NpgsqlDataSource dataSource, string tableName, int dimension)` |
| `QdrantStore` | `(QdrantClient client, int dimension)` |

### GenericRAGHook

`GenericRAGHook(IKnowledge, KnowledgeSearchOptions?, RAGMode, Func<Msg, string>?)` 在 Agent 推理前自动检索知识库。

`RAGMode` 枚举：

| 值 | 行为 |
| --- | --- |
| `Retrieval` | 将上下文追加到消息前 |
| `RetrievalQA` | 替换为 QA 提示 |
| `RetrievalOnly` | 仅存储上下文，不修改消息 |

### RAGTools 与 KnowledgeRetrievalTools

`RAGTools.CreateAll(IKnowledge)` 返回 `KnowledgeSearchTool`、`KnowledgeGetDocumentTool`、`KnowledgeAddDocumentTool`。也可单独用 `KnowledgeSearchTool` 暴露检索工具。

## 托管 RAG 客户端

各扩展包提供独立的 HTTP 客户端，**不实现** `IKnowledge`：

| 扩展 | 类 | 构造 |
| --- | --- | --- |
| Bailian | `BailianRagClient` | `(HttpClient, apiKey, baseUrl?)` |
| Dify | `DifyRagClient` | `(HttpClient, apiKey, baseUrl?)` |
| RagFlow | `RagFlowRagClient` | `(HttpClient, apiKey, baseUrl?)` |
| HayStack | `HaystackRagClient` | `(HttpClient, baseUrl)` |

使用时需自行包装成 `IKnowledge`，或直接调用其 `SearchAsync` / `RetrieveAsync` / `QueryAsync` 方法。
