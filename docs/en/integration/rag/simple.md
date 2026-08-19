# Simple Knowledge

`agentscope-extensions-rag-simple` is the "DIY end-to-end" RAG implementation: it bundles document readers, chunking strategies, embedding adapters, and five out-of-the-box vector store adapters.

Use it when: you're happy to run embeddings + vector store yourself and don't want a third-party RAG platform.

## Add the dependency

```xml
<dependency>
    <groupId>io.agentscope</groupId>
    <artifactId>agentscope-extensions-rag-simple</artifactId>
    <version>${agentscope.version}</version>
</dependency>
```

## Quickstart

```csharp
using AgentScope.Core.Embedding.DashScope.DashScopeTextEmbedding
using AgentScope.Core.Rag.Knowledge.SimpleKnowledge
using AgentScope.Core.Rag.Store.InMemoryStore
using AgentScope.Core.Rag.Model.RetrieveConfig

// 1) Embedding model
EmbeddingModel embeddings = DashScopeTextEmbedding.Builder()
    .ApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
    .ModelName("text-embedding-v3")
    .Dimensions(1024)
    .Build();

// 2) Vector store (in-process here)
VDBStoreBase store = InMemoryStore.Builder().Dimensions(1024).Build();

// 3) Assemble Knowledge
SimpleKnowledge knowledge = SimpleKnowledge.Builder()
    .EmbeddingModel(embeddings)
    .EmbeddingStore(store)
    .Build();

// 4) Ingest documents
List<Document> docs = new TikaReader().Read(input);
knowledge.AddDocuments(docs);

// 5) Retrieve
List<Document> hits = knowledge.Retrieve(
    "What is AgentScope?",
    RetrieveConfig.Builder().limit(5).scoreThreshold(0.5).Build()
);
```

## Built-in document readers

The `AgentScope.Core.Rag.Reader` package contains readers for common formats; each produces `List<Document>`:

| Reader | Input |
| --- | --- |
| `TextReader` | Plain text |
| `PDFReader` | PDF (PDFBox-backed) |
| `WordReader` | Microsoft Word documents |
| `ImageReader` | Images, paired with multimodal embeddings |
| `TikaReader` | Generic Apache Tika fallback |
| `ExternalApiReader` | External parser APIs (OCR / custom pipelines) |

The resulting `Document` objects already carry metadata; pair with `TextChunker` and `SplitStrategy` for chunking.

## Built-in embedding providers

| Class | Service | Mode |
| --- | --- | --- |
| `DashScopeTextEmbedding` | Alibaba Cloud DashScope | Text |
| `DashScopeMultiModalEmbedding` | Alibaba Cloud DashScope | Multimodal (text/image) |
| `OpenAITextEmbedding` | OpenAI-compatible API | Text |
| `OllamaTextEmbedding` | Local Ollama | Text |

Implement `EmbeddingModel` to add your own.

## Built-in vector stores

| Implementation | Deployment |
| --- | --- |
| `InMemoryStore` | In-process (dev / testing) |
| `PgVectorStore` | PostgreSQL + pgvector |
| `MilvusStore` | Milvus |
| `QdrantStore` | Qdrant |
| `ElasticsearchStore` | Elasticsearch (`dense_vector`) |

Switching stores is a one-line change: pass a different `VDBStoreBase` to `SimpleKnowledge.Builder().EmbeddingStore(...)`.

## Retrieval parameters

`RetrieveConfig` controls retrieval:

| Field | Notes |
| --- | --- |
| `limit` | Top-K |
| `scoreThreshold` | Minimum score (0–1) |
| `metadata` | Filter by document metadata |

## Wire into an Agent

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("Assistant")
    .Model(model)
    .Knowledge(knowledge)
    .RagMode(RAGMode.AGENTIC)   // Agent decides when to retrieve
    .Build();
```
