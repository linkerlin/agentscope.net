# 本地 RAG（Core 体系）

使用 `AgentScope.Core.RAG` 构建本地 RAG，无需第三方平台。

## 快速上手

```csharp
using AgentScope.Core.RAG;

// 1. Embedding 生成器
var embeddingGen = new OpenAIEmbeddingGenerator(
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
    model: "text-embedding-3-small",
    dimension: 1536
);

// 2. 内存向量存储（实现 IKnowledge）
var knowledge = new InMemoryVectorStore(embeddingGen);

// 3. 添加文档
string docId = await knowledge.AddDocumentAsync(new KnowledgeDocument
{
    Title = "AgentScope 介绍",
    Content = "AgentScope 是一个 .NET Agent 框架..."
});

// 4. 检索
var results = await knowledge.SearchAsync("什么是 AgentScope？", new KnowledgeSearchOptions
{
    TopK = 5,
    MinScore = 0.5f
});
```

## VectorStore 扩展

对接外部向量库：

```csharp
using AgentScope.Extensions.Vector.Milvus;

var milvus = new MilvusStore(httpClient, "http://localhost:19530", "my_collection", 1536);
await milvus.UpsertAsync("my_collection", "doc_1", new float[1536]);
```

| 扩展包 | 类 |
| --- | --- |
| `AgentScope.Extensions.Vector.Elasticsearch` | `ElasticsearchStore(ElasticsearchClient, indexName, dimension)` |
| `AgentScope.Extensions.Vector.Milvus` | `MilvusStore(HttpClient, baseUrl, collectionName, dimension)` |
| `AgentScope.Extensions.Vector.PgVector` | `PgVectorStore(NpgsqlDataSource, tableName, dimension)` |
| `AgentScope.Extensions.Vector.Qdrant` | `QdrantStore(QdrantClient, dimension)` |

## RAGTools 工具

```csharp
// 注册检索工具
var tools = RAGTools.CreateAll(knowledge);
var toolkit = new Toolkit();
foreach (var tool in tools) toolkit.RegisterObject(tool);

// 或单独注册搜索工具
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

可用模式：

| RAGMode | 行为 |
| --- | --- |
| `Retrieval` | 在用户消息前追加上下文 |
| `RetrievalQA` | 替换为 QA 提示 |
| `RetrievalOnly` | 检索后存储到 `Msg.Metadata["RAGContext"]`，不修改消息 |
