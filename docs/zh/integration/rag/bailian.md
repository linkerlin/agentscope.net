# 百炼 RAG

`AgentScope.Extensions.Rag.Bailian.BailianRagClient` 接入阿里云百炼知识库。

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Rag.Bailian" Version="2.0.1" />
</ItemGroup>
```

## 构造

```csharp
using AgentScope.Extensions.Rag.Bailian;

var client = new BailianRagClient(
    new HttpClient(),
    apiKey: Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"),
    baseUrl: null // 默认 https://bailian.aliyuncs.com/api/v1/rag
);
```

## 方法

```csharp
// 检索
List<string> results = await client.SearchAsync(
    indexId: "kb-xxxxx",
    query: "如何申请发票？",
    topK: 5
);

// 创建索引
string indexId = await client.CreateIndexAsync(
    name: "产品知识库",
    description: "产品 FAQ"
);
```

此类不实现 `IKnowledge`。文档管理请通过百炼控制台完成。
