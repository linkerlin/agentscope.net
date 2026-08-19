# Dify Knowledge

`agentscope-extensions-rag-dify` integrates with [Dify](https://dify.ai/) datasets, reusing knowledge bases you already maintain in Dify.

## When to use

- Your team operates content and document management in Dify.
- You want to leverage Dify's multiple retrieval modes (keyword / semantic / hybrid / full-text).

## Add the dependency

```xml
<dependency>
    <groupId>io.agentscope</groupId>
    <artifactId>agentscope-extensions-rag-dify</artifactId>
    <version>${agentscope.version}</version>
</dependency>
```

## Quickstart

```csharp
using AgentScope.Core.Rag.Integration.Dify.DifyKnowledge
using AgentScope.Core.Rag.Integration.Dify.DifyRAGConfig
using AgentScope.Core.Rag.Integration.Dify.RetrievalMode
using AgentScope.Core.Rag.Model.RetrieveConfig

DifyRAGConfig config = DifyRAGConfig.Builder()
    .ApiKey(Environment.GetEnvironmentVariable("DIFY_RAG_API_KEY"))
    .DatasetId("your-dataset-id")
    .RetrievalMode(RetrievalMode.HYBRID_SEARCH)
    .EnableRerank(true)
    .Build();

DifyKnowledge knowledge = DifyKnowledge.Builder()
    .Config(config)
    .Build();

List<Document> hits = knowledge.Retrieve(
    "How do I renew my membership?",
    RetrieveConfig.Builder().limit(5).scoreThreshold(0.5).Build()
);
```

## Retrieval modes

`RetrievalMode` selects how Dify searches the dataset:

| Enum | Description |
| --- | --- |
| `KEYWORD_SEARCH` | Keyword only |
| `SEMANTIC_SEARCH` | Vector / semantic only |
| `HYBRID_SEARCH` | Keyword + vector hybrid (recommended) |
| `FULL_TEXT_SEARCH` | Full-text |

## Self-hosted Dify

Point `baseUrl` at your deployment:

```csharp
DifyRAGConfig config = DifyRAGConfig.Builder()
    .ApiKey("dataset-xxx")
    .BaseUrl("https://dify.mycompany.com")
    .DatasetId("ds-xxxx")
    .RetrievalMode(RetrievalMode.HYBRID_SEARCH)
    .Build();
```

## Metadata filtering

Use `MetadataFilter / MetadataFilterCondition` to filter by metadata fields you've configured in Dify:

```csharp
DifyRAGConfig config = DifyRAGConfig.Builder()
    .ApiKey(apiKey)
    .DatasetId(datasetId)
    .RetrievalMode(RetrievalMode.HYBRID_SEARCH)
    .MetadataFilter(MetadataFilter.Builder()
        .Conditions(new List<MetadataFilterCondition> {
            MetadataFilterCondition.Builder()
                .Name("category").ComparisonOperator("=")
                .Value(new List<string> { "faq" }).Build() })
        .LogicalOperator("and")
        .Build())
    .Build();
```

## Retrieval only

`addDocuments(...)` is unsupported — use the Dify console: log in → Knowledge → choose dataset → upload documents → wait for indexing. This matches Bailian, HayStack, and RAGFlow.

## Key parameters

| Field | Notes |
| --- | --- |
| `apiKey` | Dify dataset API key (required) |
| `datasetId` | Dataset ID (required) |
| `baseUrl` | Default `https://api.dify.ai/v1`, override for self-hosted |
| `retrievalMode` | See table above |
| `enableRerank` | Enable rerank |
| `metadataFilter` | Metadata filter conditions |
