# Simple Knowledge

`agentscope-extensions-rag-simple` 提供一个"自己掌控全部链路"的 RAG 实现：自带文档读取器、分块策略、Embedding 模型适配、以及 5 个开箱即用的向量库适配器。

适合：你愿意自己跑 embedding + 向量库，不想接入第三方 RAG 平台。

## 添加依赖

```xml
<dependency>
    <groupId>io.agentscope</groupId>
    <artifactId>agentscope-extensions-rag-simple</artifactId>
    <version>${agentscope.version}</version>
</dependency>
```

## 快速上手

```csharp
using AgentScope.Core.Embedding.DashScope.DashScopeTextEmbedding
using AgentScope.Core.Rag.Knowledge.SimpleKnowledge
using AgentScope.Core.Rag.Store.InMemoryStore
using AgentScope.Core.Rag.Model.RetrieveConfig

// 1) Embedding 模型
EmbeddingModel embeddings = DashScopeTextEmbedding.Builder()
    .ApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
    .ModelName("text-embedding-v3")
    .Dimensions(1024)
    .Build();

// 2) 向量库（这里用进程内的实现）
VDBStoreBase store = InMemoryStore.Builder().Dimensions(1024).Build();

// 3) 组装 Knowledge
SimpleKnowledge knowledge = SimpleKnowledge.Builder()
    .EmbeddingModel(embeddings)
    .EmbeddingStore(store)
    .Build();

// 4) 写入文档
List<Document> docs = new TikaReader().Read(input);
knowledge.AddDocuments(docs);

// 5) 检索
List<Document> hits = knowledge.Retrieve(
    "什么是 AgentScope？",
    RetrieveConfig.Builder().limit(5).scoreThreshold(0.5).Build()
);
```

## 内置文档读取器

`AgentScope.Core.Rag.Reader` 包提供了一组常见格式的 Reader，全部产出 `List<Document>`：

| Reader | 输入 |
| --- | --- |
| `TextReader` | 纯文本 |
| `PDFReader` | PDF（基于 PDFBox） |
| `WordReader` | Word 文档 |
| `ImageReader` | 图片，配合多模态 embedding 使用 |
| `TikaReader` | Apache Tika 通用解析（兜底） |
| `ExternalApiReader` | 调外部 API 解析（OCR / 自定义流水线） |

读取出来的 `Document` 已经带有元数据，配合 `TextChunker` 与 `SplitStrategy` 做分块。

## 内置 Embedding 提供方

| 类 | 服务 | 模式 |
| --- | --- | --- |
| `DashScopeTextEmbedding` | 阿里云百炼 DashScope | 文本 |
| `DashScopeMultiModalEmbedding` | 阿里云百炼 DashScope | 多模态（文本/图像） |
| `OpenAITextEmbedding` | OpenAI 兼容接口 | 文本 |
| `OllamaTextEmbedding` | Ollama 本地 | 文本 |

也可以实现 `EmbeddingModel` 自行扩展。

## 内置向量库适配

| 实现 | 部署 |
| --- | --- |
| `InMemoryStore` | 进程内（开发/测试用） |
| `PgVectorStore` | PostgreSQL + pgvector |
| `MilvusStore` | Milvus |
| `QdrantStore` | Qdrant |
| `ElasticsearchStore` | Elasticsearch（dense_vector） |

切换向量库只需要换一个 `VDBStoreBase` 实现，传给 `SimpleKnowledge.Builder().EmbeddingStore(...)`。

## 检索参数

`RetrieveConfig` 控制检索行为：

| 字段 | 说明 |
| --- | --- |
| `limit` | TopK |
| `scoreThreshold` | 最低分数阈值（0~1） |
| `metadata` | 按文档 metadata 做过滤 |

## 与 Agent 集成

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("Assistant")
    .Model(model)
    .Knowledge(knowledge)
    .RagMode(RAGMode.AGENTIC)   // Agent 自行决定何时检索
    .Build();
```
