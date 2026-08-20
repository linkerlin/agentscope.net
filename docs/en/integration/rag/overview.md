# RAG Overview

## Core RAG

Located in `AgentScope.Core.RAG`:

### IKnowledge Interface

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

`InMemoryVectorStore(IEmbeddingGenerator? embeddingGenerator = null)` implements `IKnowledge`. With an `IEmbeddingGenerator` injected, it enables vector-based semantic search.

### VectorStore Extensions

`AgentScope.Extensions.Vector.IVectorStore`:

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

| Implementation | Constructor |
| --- | --- |
| `ElasticsearchStore` | `(ElasticsearchClient client, string indexName, int dimension)` |
| `MilvusStore` | `(HttpClient httpClient, string baseUrl, string collectionName, int dimension)` |
| `PgVectorStore` | `(NpgsqlDataSource dataSource, string tableName, int dimension)` |
| `QdrantStore` | `(QdrantClient client, int dimension)` |

### GenericRAGHook

`GenericRAGHook(IKnowledge, KnowledgeSearchOptions?, RAGMode, Func<Msg, string>?)` retrieves context before Agent reasoning.

`RAGMode`:

| Value | Behavior |
| --- | --- |
| `Retrieval` | Prepend context to the message |
| `RetrievalQA` | Replace with a QA prompt |
| `RetrievalOnly` | Store context in `Msg.Metadata`, don't modify message |

### RAGTools & KnowledgeRetrievalTools

`RAGTools.CreateAll(IKnowledge)` returns `KnowledgeSearchTool`, `KnowledgeGetDocumentTool`, `KnowledgeAddDocumentTool`. Use `KnowledgeSearchTool` individually for search-only.

## Managed RAG Clients

Extension packages provide independent HTTP clients, **not implementing** `IKnowledge`:

| Extension | Class | Constructor |
| --- | --- | --- |
| Bailian | `BailianRagClient` | `(HttpClient, apiKey, baseUrl?)` |
| Dify | `DifyRagClient` | `(HttpClient, apiKey, baseUrl?)` |
| RagFlow | `RagFlowRagClient` | `(HttpClient, apiKey, baseUrl?)` |
| HayStack | `HaystackRagClient` | `(HttpClient, baseUrl)` |

Wrap them as `IKnowledge` or call their methods directly.
