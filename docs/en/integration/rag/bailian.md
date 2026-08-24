# Bailian RAG

`AgentScope.Extensions.Rag.Bailian.BailianRagClient` integrates with Alibaba Cloud Bailian Knowledge Base.

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Rag.Bailian" Version="2.0.1" />
</ItemGroup>
```

## Construction

```csharp
using AgentScope.Extensions.Rag.Bailian;

var client = new BailianRagClient(
    new HttpClient(),
    apiKey: Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"),
    baseUrl: null // defaults to https://bailian.aliyuncs.com/api/v1/rag
);
```

## Methods

```csharp
// Search
List<string> results = await client.SearchAsync(
    indexId: "kb-xxxxx",
    query: "How do I request an invoice?",
    topK: 5
);

// Create index
string indexId = await client.CreateIndexAsync(
    name: "product-kb",
    description: "Product FAQ"
);
```

This class does not implement `IKnowledge`. Document management goes through the Bailian console.
