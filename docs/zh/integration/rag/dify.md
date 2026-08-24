# Dify RAG

`AgentScope.Extensions.Rag.Dify.DifyRagClient` 接入 [Dify](https://dify.ai/) 数据集。

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Rag.Dify" Version="2.0.1" />
</ItemGroup>
```

## 构造

```csharp
using AgentScope.Extensions.Rag.Dify;

var client = new DifyRagClient(
    new HttpClient(),
    apiKey: Environment.GetEnvironmentVariable("DIFY_RAG_API_KEY"),
    baseUrl: null // 默认 https://api.dify.ai/v1
);
```

## 方法

```csharp
// 检索数据集
List<string> results = await client.RetrieveAsync(
    datasetId: "ds-xxxxx",
    query: "如何续费会员？",
    topK: 5
);
```

此类不实现 `IKnowledge`。文档管理请通过 Dify 控制台完成。
