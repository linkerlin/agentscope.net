# RAGFlow RAG

`AgentScope.Extensions.Rag.RagFlow.RagFlowRagClient` 接入 [RAGFlow](https://ragflow.io/)。

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Rag.RagFlow" Version="2.0.1" />
</ItemGroup>
```

## 构造

```csharp
using AgentScope.Extensions.Rag.RagFlow;

var client = new RagFlowRagClient(
    new HttpClient(),
    apiKey: "ragflow-xxxxxxxx",
    baseUrl: null // 默认 https://api.ragflow.io/v1
);
```

## 方法

```csharp
// 检索文档块
List<string> chunks = await client.SearchAsync(
    datasetId: "kb-xxxxx",
    query: "AI 是什么？",
    topK: 5
);

// 上传文档
string docId = await client.UploadDocumentAsync(
    datasetId: "kb-xxxxx",
    fileName: "report.pdf",
    content: File.ReadAllBytes("report.pdf")
);
```

此类不实现 `IKnowledge`。
