# Memory

AgentScope provides a multi-layer memory system:

- **Core memory interfaces** `ILongTermMemory` and `IMemory` in `AgentScope.Core.Memory`.
- **Harness memory** see [Harness docs](../../docs/harness/memory.md).
- **Managed memory clients** via extension packages (do not implement `ILongTermMemory`; requires manual wrapping).

| Extension | Backend | Use case |
| --- | --- | --- |
| [Mem0](mem0.md) | [Mem0](https://mem0.ai/) Platform / self-hosted | General semantic memory, multi-tenant isolation |
| [ReMe](reme.md) | Self-hosted ReMe | Trajectory summarization, workspace isolation |
| [Bailian](bailian.md) | Alibaba Cloud Bailian | Cloud-managed, rerank/judge/rewrite |
