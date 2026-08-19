# Bailian Knowledge

`agentscope-extensions-rag-bailian` integrates Alibaba Cloud Bailian Knowledge Base — embeddings, indexing, and retrieval are all managed by Bailian. The Agent only sends the query and receives documents back.

## When to use

- Your documents are already uploaded and processed in the Bailian console.
- You want enterprise features: rerank, filtering, and structured / unstructured / image KBs.
- You don't want to run your own vector store.

## Add the dependency

```xml
<dependency>
    <groupId>io.agentscope</groupId>
    <artifactId>agentscope-extensions-rag-bailian</artifactId>
    <version>${agentscope.version}</version>
</dependency>
```

## Quickstart

```csharp
using AgentScope.Core.Rag.Integration.Bailian.BailianConfig
using AgentScope.Core.Rag.Integration.Bailian.BailianKnowledge
using AgentScope.Core.Rag.Model.RetrieveConfig

BailianConfig config = BailianConfig.Builder()
    .AccessKeyId(Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_ID"))
    .AccessKeySecret(Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_SECRET"))
    .WorkspaceId("llm-xxxxxx")
    .IndexId("kb-xxxxxx")
    .Build();

BailianKnowledge knowledge = BailianKnowledge.Builder()
    .Config(config)
    .Build();

List<Document> hits = knowledge.Retrieve(
    "How do I request an invoice?",
    RetrieveConfig.Builder().limit(5).scoreThreshold(0.5).Build()
);
```

## Wire into an Agent

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("Assistant")
    .Model(chatModel)
    .Knowledge(knowledge)
    .RagMode(RAGMode.AGENTIC)
    .Build();
```

Or expose it as a tool:

```csharp
KnowledgeRetrievalTools tools = new KnowledgeRetrievalTools(knowledge);
Toolkit toolkit = new Toolkit();
toolkit.RegisterObject(tools);
```

## Rerank / rewrite

`BailianConfig.Builder()` accepts optional `rerankConfig(...)` and `rewriteConfig(...)`:

```csharp
BailianConfig config = BailianConfig.Builder()
    .AccessKeyId(ak).AccessKeySecret(sk)
    .WorkspaceId("llm-xxx").IndexId("kb-xxx")
    .RerankConfig(RerankConfig.Builder().Enable(true).TopN(5).Build())
    .RewriteConfig(RewriteConfig.Builder().Enable(true).Build())
    .Build();
```

When enabled, Bailian re-ranks initial recall results or rewrites the query for better relevance — at the cost of more latency and quota usage. Turn on what you actually need.

## Retrieval only

`BailianKnowledge.AddDocuments(...)` is unsupported — use the Bailian console or platform SDK to manage documents. This is consistent with Dify, HayStack, and RAGFlow integrations: third-party RAG platforms keep ingestion responsibility, the .NET side only reads.

## Configuration

| Field | Notes |
| --- | --- |
| `accessKeyId / accessKeySecret` | Alibaba Cloud credentials (required) |
| `workspaceId` | Bailian workspace ID (required) |
| `indexId` | KB index ID (required) |
| `rerankConfig` | Rerank toggle and parameters |
| `rewriteConfig` | Query rewrite toggle and parameters |
