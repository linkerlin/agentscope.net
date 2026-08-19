# Bailian Knowledge

`agentscope-extensions-rag-bailian` 接入阿里云百炼知识库，所有 embedding、索引、检索都由百炼托管。Agent 这边只负责把 query 抛过去、把文档拿回来。

## 何时使用

- 文档已经在百炼控制台上传/解析完成。
- 想要企业级特性：rerank、过滤、结构化/非结构化/图片三类知识库。
- 不想自维护向量库。

## 添加依赖

```xml
<dependency>
    <groupId>io.agentscope</groupId>
    <artifactId>agentscope-extensions-rag-bailian</artifactId>
    <version>${agentscope.version}</version>
</dependency>
```

## 快速上手

```csharp
using AgentScope.Core.Rag.Integration.Bailian.BailianConfig
using AgentScope.Core.Rag.Integration.Bailian.BailianKnowledge
using AgentScope.Core.Rag.Model.RetrieveConfig

BailianConfig config = BailianConfig.Builder()
    .AccessKeyId(Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_ID"))
    .AccessKeySecret(Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_SECRET"))
    .WorkspaceId("llm-xxxxxx")
    .IndexId("kb-xxxxxx")
    .Build();

BailianKnowledge knowledge = BailianKnowledge.Builder()
    .Config(config)
    .Build();

List<Document> hits = knowledge.Retrieve(
    "如何申请发票？",
    RetrieveConfig.Builder().limit(5).scoreThreshold(0.5).Build()
);
```

## 与 Agent 集成

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("Assistant")
    .Model(chatModel)
    .Knowledge(knowledge)
    .RagMode(RAGMode.AGENTIC)
    .Build();
```

或者通过工具暴露：

```csharp
KnowledgeRetrievalTools tools = new KnowledgeRetrievalTools(knowledge);
Toolkit toolkit = new Toolkit();
toolkit.RegisterObject(tools);
```

## rerank / rewrite 配置

`BailianConfig.Builder()` 可选传入 `rerankConfig(...)` 与 `rewriteConfig(...)`：

```csharp
BailianConfig config = BailianConfig.Builder()
    .AccessKeyId(ak).AccessKeySecret(sk)
    .WorkspaceId("llm-xxx").IndexId("kb-xxx")
    .RerankConfig(RerankConfig.Builder().Enable(true).TopN(5).Build())
    .RewriteConfig(RewriteConfig.Builder().Enable(true).Build())
    .Build();
```

启用后百炼侧会在原始召回上再做一遍重排或 query 改写，提升相关性，但延迟和费用也会上升，按需打开。

## 仅支持检索

`BailianKnowledge.AddDocuments(...)` 不可用——文档管理请通过百炼控制台或百炼平台 SDK 完成。这是和 Dify、HayStack、RAGFlow 一致的设计：第三方 RAG 平台保留索引能力，.NET 侧只做读取。

## 配置参数

| 配置 | 说明 |
| --- | --- |
| `accessKeyId / accessKeySecret` | 阿里云访问凭证（必填） |
| `workspaceId` | 百炼业务空间 ID（必填） |
| `indexId` | 知识库索引 ID（必填） |
| `rerankConfig` | rerank 开关与参数 |
| `rewriteConfig` | query rewrite 开关与参数 |
