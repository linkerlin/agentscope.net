---
title: "What is AgentScope 2.0?"
description: "Harness engineering, stateless multi-session, pluggable storage and protocol layers."
---

AgentScope .NET 2.0 is positioned as a **production-oriented agent runtime**: a core reasoning engine (`EnhancedReActAgent`) + an engineering shell (`HarnessAgent`) + pluggable storage / channel / protocol extensions.

## 1 · Harness Engineering

`AgentScope.Harness` provides all the engineering facilities needed for long-running agents in one package:

- **Workspace**: `AGENTS.md` / `MEMORY.md` / `KNOWLEDGE.md` / `skills/` / `subagents/` all expressed as on-disk Markdown, automatically injected as system prompts each turn;
- **Middleware Pipeline**: `IHarnessMiddleware` onion model (four hooks: turn / model call / tool execution / system prompt), 15+ built-in middlewares sorted by `Order`;
- **Context Management**: `CompactionMiddleware` threshold marking + `ConversationCompactor` truncation / pruning / summarization + `ToolResultEviction` large result offloading;
- **Memory**: session transcripts (JSONL), `MemoryFlushManager` flushing, `MemoryConsolidator` periodic consolidation;
- **Sub-agents / Teams**: `SubagentDeclaration` declarative sub-agents (remote-capable), `LocalTeamClient` task collaboration;
- **Filesystem Abstraction**: `IFilesystem` four implementations: local / overlay / composite / sandbox, `LocalFsMode` three-tier isolation.

## 2 · Stateless Multi-Session

`EnhancedReActAgent` and `HarnessAgent` are both **stateless engines**:

- A single instance serves any `(UserId, SessionId)` combination; each call is identified via `RuntimeContext` (a record with `With*` derivation);
- Memory is replaceable via `IMemory`: `MemoryBase` (in-memory), `SqliteMemory` (SQLite persistence), `StateBackedMemory` (auto-writes to `IAgentStateStore`);
- `SessionManager` + `IStateModule` (`SaveTo` / `LoadFrom` / `LoadIfExists`) supports session-level state save and restore;
- Distributed state: `AgentScope.Extensions.Store.*` (Redis / MySQL / PostgreSQL / OSS / COS) all implement `IAgentStateStore`.

## 3 · Pluggable Extensions

| Extension Family | Interface / Base Class | Description |
|------------------|------------------------|-------------|
| `Store.*` | `AgentScope.Core.State.IAgentStateStore` | Distributed state storage |
| `Vector.*` (Elasticsearch / Milvus / PgVector / Qdrant) | `AgentScope.Extensions.Vector.IVectorStore` | Vector retrieval |
| `Skill.*` (Git / MySql / PostgreSql) | `AgentScope.Extensions.Skill.ISkillRepository` | Skill repository backend |
| `Channel.*` (DingTalk / Feishu / WeCom / GitHub / GitLab) | `AgentScope.Extensions.Channel.IChannel` | Instant messaging channels |
| `Sandbox.*` (Docker / E2B / Daytona / AgentRun / Kubernetes) | `AgentScope.Extensions.Sandbox.ISandbox` | Execution isolation environment |
| `Mem.*` (Mem0 / ReMe / Bailian) | Standalone HTTP client | Managed long-term memory |
| `Rag.*` (Dify / RagFlow / Haystack / Bailian) | Standalone HTTP client | Managed RAG service |
| `Scheduler.*` (Quartz / XxlJob) | `AgentScope.Extensions.Scheduler.IAgentScheduler` | Scheduled agent triggering |
| `Tracing.OpenTelemetry` | `AgentScope.Harness.Middleware.IHarnessMiddleware` | OTLP distributed tracing |
| `Nacos*` | `AgentScope.Core.Service.Discovery.IAgentRegistry` | Service registration / discovery / prompt / skill repository |

Protocol layers (A2A / Agent Protocol / AgUI) and model layers (A2A client, AgUI adapter) are built into `AgentScope.Core`.

## Migration Notes

The v1-era `ReActAgent` (and its Builder) are marked `[Obsolete]` in 2.0; v1 APIs such as `RuntimeContext.Builder()`, string model IDs, and `StreamAsync` are no longer used. See [Quickstart](./quickstart.md) for the recommended coding style, and [Change Log](./change-log.md) for the full list of changes.
