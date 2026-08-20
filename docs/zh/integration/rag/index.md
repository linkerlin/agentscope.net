# RAG 知识库

AgentScope 提供两套 RAG 方案：

- **Core RAG 体系**：`IKnowledge` + `InMemoryVectorStore` + `GenericRAGHook` + `RAGTools`，位于 `AgentScope.Core.RAG`。
- **托管 RAG 客户端**：通过扩展包接入外部 RAG 平台（不实现 `IKnowledge`）。

| 类型 | 文档 |
| --- | --- |
| 本地 RAG（Core 体系） | [simple](simple.md) |
| 阿里云百炼 | [bailian](bailian.md) |
| Dify | [dify](dify.md) |
| RAGFlow | [ragflow](ragflow.md) |
| HayStack | [haystack](haystack.md) |
