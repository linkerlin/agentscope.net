# HayStack RAG

`AgentScope.Extensions.Rag.Haystack.HaystackRagClient` 接入 [HayStack](https://haystack.deepset.ai/) 服务。

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Rag.Haystack" Version="2.0.1" />
</ItemGroup>
```

## 构造

```csharp
using AgentScope.Extensions.Rag.Haystack;

var client = new HaystackRagClient(
    new HttpClient(),
    baseUrl: "http://localhost:8080"
);
```

构造函数：`(HttpClient http, string baseUrl)` — 无 `apiKey` 参数。

## 方法

```csharp
// 查询管道
List<string> results = await client.QueryAsync(
    pipelineId: "my-pipeline",
    query: "What is AI?",
    topK: 5
);

// 索引文档
string docId = await client.IndexDocumentAsync(
    pipelineId: "my-pipeline",
    text: "AgentScope is a .NET agent framework."
);
```

此类不实现 `IKnowledge`。
