# Dify Knowledge

`agentscope-extensions-rag-dify` 接入 [Dify](https://dify.ai/) 的数据集（Dataset）API，复用 Dify 上已经维护的知识库。

## 何时使用

- 团队已经在 Dify 上做内容运营和文档管理。
- 想直接复用 Dify 的多种检索模式（关键词 / 语义 / 混合 / 全文）。

## 添加依赖

```xml
<dependency>
    <groupId>io.agentscope</groupId>
    <artifactId>agentscope-extensions-rag-dify</artifactId>
    <version>${agentscope.version}</version>
</dependency>
```

## 快速上手

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
    "如何续费会员？",
    RetrieveConfig.Builder().limit(5).scoreThreshold(0.5).Build()
);
```

## 检索模式

`RetrievalMode` 决定 Dify 在数据集上的检索方式：

| 枚举 | 说明 |
| --- | --- |
| `KEYWORD_SEARCH` | 仅关键词检索 |
| `SEMANTIC_SEARCH` | 仅向量语义检索 |
| `HYBRID_SEARCH` | 关键词 + 向量混合（推荐） |
| `FULL_TEXT_SEARCH` | 全文检索 |

## 自托管 Dify

如果你部署了自己的 Dify，把 `baseUrl` 指过去：

```csharp
DifyRAGConfig config = DifyRAGConfig.Builder()
    .ApiKey("dataset-xxx")
    .BaseUrl("https://dify.mycompany.com")
    .DatasetId("ds-xxxx")
    .RetrievalMode(RetrievalMode.HYBRID_SEARCH)
    .Build();
```

## metadata 过滤

通过 `MetadataFilter / MetadataFilterCondition` 可以按 Dify 上配置的元数据字段过滤：

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

## 仅支持检索

`addDocuments(...)` 不可用——文档管理请去 Dify 控制台：登录 → 知识 → 选择数据集 → 上传文档 → 等待索引完成。这与 Bailian、HayStack、RAGFlow 保持一致。

## 关键参数

| 字段 | 说明 |
| --- | --- |
| `apiKey` | Dify 数据集 API Key（必填） |
| `datasetId` | 数据集 ID（必填） |
| `baseUrl` | 默认 `https://api.dify.ai/v1`，自托管时改为你的地址 |
| `retrievalMode` | 见上表 |
| `enableRerank` | 是否启用 rerank |
| `metadataFilter` | 元数据过滤条件 |
