# RAGFlow RAG

`AgentScope.Extensions.Rag.RagFlow.RagFlowRagClient` integrates with [RAGFlow](https://ragflow.io/).

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Rag.RagFlow" Version="2.0.1" />
</ItemGroup>
```

## Construction

```csharp
using AgentScope.Extensions.Rag.RagFlow;

var client = new RagFlowRagClient(
    new HttpClient(),
    apiKey: "ragflow-xxxxxxxx",
    baseUrl: null // defaults to https://api.ragflow.io/v1
);
```

## Methods

```csharp
// Search chunks
List<string> chunks = await client.SearchAsync(
    datasetId: "kb-xxxxx",
    query: "What is AI?",
    topK: 5
);

// Upload document
string docId = await client.UploadDocumentAsync(
    datasetId: "kb-xxxxx",
    fileName: "report.pdf",
    content: File.ReadAllBytes("report.pdf")
);
```

This class does not implement `IKnowledge`.
