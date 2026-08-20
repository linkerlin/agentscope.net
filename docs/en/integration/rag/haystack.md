# HayStack RAG

`AgentScope.Extensions.Rag.Haystack.HaystackRagClient` connects to a [HayStack](https://haystack.deepset.ai/) service.

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Rag.Haystack" Version="2.0.1" />
</ItemGroup>
```

## Construction

```csharp
using AgentScope.Extensions.Rag.Haystack;

var client = new HaystackRagClient(
    new HttpClient(),
    baseUrl: "http://localhost:8080"
);
```

Constructor: `(HttpClient http, string baseUrl)` — no `apiKey` parameter.

## Methods

```csharp
// Query a pipeline
List<string> results = await client.QueryAsync(
    pipelineId: "my-pipeline",
    query: "What is AI?",
    topK: 5
);

// Index a document
string docId = await client.IndexDocumentAsync(
    pipelineId: "my-pipeline",
    text: "AgentScope is a .NET agent framework."
);
```

This class does not implement `IKnowledge`.
