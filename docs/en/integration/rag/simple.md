# Local RAG (Core)

Use `AgentScope.Core.RAG` to build local RAG without third-party platforms.

## Quickstart

```csharp
using AgentScope.Core.RAG;

// 1. Embedding generator
var embeddingGen = new OpenAIEmbeddingGenerator(
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
    model: "text-embedding-3-small",
    dimension: 1536
);

// 2. In-memory vector store (implements IKnowledge)
var knowledge = new InMemoryVectorStore(embeddingGen);

// 3. Add documents
string docId = await knowledge.AddDocumentAsync(new KnowledgeDocument
{
    Title = "AgentScope Introduction",
    Content = "AgentScope is a .NET agent framework..."
});

// 4. Search
var results = await knowledge.SearchAsync("What is AgentScope?", new KnowledgeSearchOptions
{
    TopK = 5,
    MinScore = 0.5f
});
```

## VectorStore Extensions

Connect to external vector stores:

```csharp
using AgentScope.Extensions.Vector.Milvus;

var milvus = new MilvusStore(httpClient, "http://localhost:19530", "my_collection", 1536);
await milvus.UpsertAsync("my_collection", "doc_1", new float[1536]);
```

| Package | Class |
| --- | --- |
| `AgentScope.Extensions.Vector.Elasticsearch` | `ElasticsearchStore(ElasticsearchClient, indexName, dimension)` |
| `AgentScope.Extensions.Vector.Milvus` | `MilvusStore(HttpClient, baseUrl, collectionName, dimension)` |
| `AgentScope.Extensions.Vector.PgVector` | `PgVectorStore(NpgsqlDataSource, tableName, dimension)` |
| `AgentScope.Extensions.Vector.Qdrant` | `QdrantStore(QdrantClient, dimension)` |

## RAGTools

```csharp
// Register all tools
var tools = RAGTools.CreateAll(knowledge);
var toolkit = new Toolkit();
foreach (var tool in tools) toolkit.RegisterObject(tool);

// Or just the search tool
var searchTool = RAGTools.CreateSearchTool(knowledge);
```

## GenericRAGHook

```csharp
var hook = new RAGHookBuilder()
    .WithKnowledge(knowledge)
    .WithTopK(3)
    .WithMode(RAGMode.Retrieval)
    .Build();
```

RAGMode:

| RAGMode | Behavior |
| --- | --- |
| `Retrieval` | Prepend context before user message |
| `RetrievalQA` | Replace with QA prompt |
| `RetrievalOnly` | Store in `Msg.Metadata["RAGContext"]`, leave message unchanged |
