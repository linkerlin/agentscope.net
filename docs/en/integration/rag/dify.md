# Dify RAG

`AgentScope.Extensions.Rag.Dify.DifyRagClient` integrates with [Dify](https://dify.ai/) datasets.

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Rag.Dify" Version="2.0.1" />
</ItemGroup>
```

## Construction

```csharp
using AgentScope.Extensions.Rag.Dify;

var client = new DifyRagClient(
    new HttpClient(),
    apiKey: Environment.GetEnvironmentVariable("DIFY_RAG_API_KEY"),
    baseUrl: null // defaults to https://api.dify.ai/v1
);
```

## Methods

```csharp
// Retrieve from dataset
List<string> results = await client.RetrieveAsync(
    datasetId: "ds-xxxxx",
    query: "How do I renew my membership?",
    topK: 5
);
```

This class does not implement `IKnowledge`. Document management goes through the Dify console.
