# 记忆（Memory）

AgentScope 提供多层记忆体系：

- **Core 记忆接口** `ILongTermMemory` 与 `IMemory`，位于 `AgentScope.Core.Memory` 命名空间。
- **Harness 记忆体系** 见 [Harness 文档](../../docs/harness/memory.md)。
- **托管记忆客户端** 通过扩展包提供（不实现 Core 接口），需自行适配 `ILongTermMemory`。

| 扩展 | 后端 | 适合场景 |
| --- | --- | --- |
| [Mem0](mem0.md) | [Mem0](https://mem0.ai/) 平台 / 自托管 | 通用语义记忆，多租户隔离 |
| [ReMe](reme.md) | 自托管 ReMe 服务 | 轨迹摘要，工作区隔离 |
| [百炼](bailian.md) | 阿里云百炼记忆服务 | 云端托管，rerank/judge/rewrite |
