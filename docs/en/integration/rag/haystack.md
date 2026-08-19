# HayStack Knowledge

`agentscope-extensions-rag-haystack` connects AgentScope to a [HayStack](https://haystack.deepset.ai/) RAG service. Document management and indexing happen on the HayStack side; AgentScope only invokes its retrieval API.

## When to use

- You already run RAG on HayStack (indexing pipeline, ChromaDB, rerankers, etc.).
- You want to reuse HayStack's end-to-end retrieval capability.

## Add the dependency

```xml
<dependency>
    <groupId>io.agentscope</groupId>
    <artifactId>agentscope-extensions-rag-haystack</artifactId>
    <version>${agentscope.version}</version>
</dependency>
```

## Quickstart

```csharp
using AgentScope.Core.Rag.Integration.HayStack.HayStackConfig
using AgentScope.Core.Rag.Integration.HayStack.HayStackKnowledge
using AgentScope.Core.Rag.Model.RetrieveConfig

HayStackConfig config = HayStackConfig.Builder()
    .BaseUrl("http://localhost:8080")  // your HayStack service
    .TopK(10)
    .Build();

HayStackKnowledge knowledge = HayStackKnowledge.Builder()
    .Config(config)
    .Build();

List<Document> hits = knowledge.Retrieve(
    "What is AI?",
    RetrieveConfig.Builder().limit(5).Build()
);
```

## Wire into an Agent

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Knowledge(knowledge)
    .RagMode(RAGMode.AGENTIC)
    .Build();
```

## Document management is not handled here

`addDocuments(...)` throws `UnsupportedOperationException`. To add or update documents:

1. Place source files in HayStack's pipeline source directory.
2. Trigger / re-run HayStack's indexing pipeline.
3. Once indexing completes, this plugin can retrieve the new documents.

This separation prevents inconsistent indexing states across two sides.

## Key parameters

| Field | Notes |
| --- | --- |
| `baseUrl` | HayStack service URL (required) |
| `topK` | Default top-K |
| `filterPolicy` | Filter policy (see `FilterPolicy`) |

`HayStackConfig` also exposes timeouts, custom headers, and API keys to fit your HayStack deployment's auth scheme.
