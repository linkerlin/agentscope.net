# 会话状态管理

AgentScope 提供一套完整的会话（Session）状态管理机制，支持 Agent 运行时上下文的持久化、恢复与跨会话切换。

核心概念包括：

- **`IAgentStateStore`** — 状态存储接口，定义于 `AgentScope.Core.State`。支持 `GetAsync`、`SaveAsync` 以及可选的版本化乐观并发（`GetVersionedAsync`、`SaveIfVersionAsync`）。
- **`StateBackedMemory`** — 实现了 `IMemory` 的内存包装器，所有变更自动持久化到关联的 `IAgentStateStore`。
- **`Session` / `SessionManager`** — 会话生命周期管理，支持创建、切换、暂停、恢复和删除会话。
- **`EnhancedReActAgent`** — 提供 `SaveTo(Session, sessionKey)` 与 `LoadFrom(Session, sessionKey)` 方法，支持将 Agent 状态持久化到指定会话。

## 文档

- [概览 — Session 与状态持久化](overview.md) — `Session`、`SessionManager`、`StateBackedMemory`、`EnhancedReActAgent` 用法详解。
- [Redis 后端](redis.md) — 使用 `RedisAgentStateStore` 实现分布式会话状态。
- [MySQL 后端](mysql.md) — 使用 `MySqlAgentStateStore` 实现持久化。
- [OSS 后端](oss.md) — 使用 `OssAgentStateStore` 接入阿里云对象存储。

如需同时覆盖 Agent 状态、工作区文件、沙箱快照和并发锁，请参阅[分布式存储总览](../distributed/index.md)。
