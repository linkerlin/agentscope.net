---
title: "Release Notes"
description: "AgentScope .NET version release history"
---

## 2.0.1 (Current)

First stable 2.0 release. Target framework `net10.0`.

**Core (AgentScope.Core)**

- `EnhancedReActAgent` replaces `ReActAgent` (the latter marked `[Obsolete]`);
- Model system: OpenAI / DashScope / Anthropic / Gemini / DeepSeek / Ollama / Mock all built-in, unified `IModel` + `IStreamingChatModel` streaming interface;
- Messages: `Msg.Builder()`, `ContentBlock` record hierarchy (text / image / audio / video / tool call / tool result / thinking block);
- Events: `Event` + `EventType` coarse-grained streaming events, `AgentEvent` fine-grained record hierarchy retained for protocol layer;
- Tools: `[Tool]` / `[ToolParam]` attribute registration, `Toolkit` grouping, `ToolExecutor` retry/timeout, built-in file / shell / search / code execution tools;
- MCP: `McpClientBuilder` (Stdio / Streamable HTTP / SSE) + `McpManager` tool discovery;
- Permission: `PermissionEngine` six-step decision state machine + HITL confirmation callback;
- Hook: `HookManager` + 11 lifecycle callbacks;
- State: `IAgentStateStore` (InMemory / JsonFile), `Session` / `SessionManager`, `IStateModule` (SaveTo / LoadFrom / LoadIfExists), `StateBackedMemory`, `SqliteMemory`, `InMemoryLongTermMemory`;
- Structured output: `GenerateStructuredOutputAsync<T>`;
- Tracing: `AgentScope.Core.Tracing` Jsonl export;
- Protocol: A2A client / server, AgUI adapter, Agent Protocol task client;
- Service discovery: `IAgentRegistry` (InMemory / Nacos extension).

**Harness (AgentScope.Harness)**

- `HarnessAgent` + `HarnessAgentBuilder` (20+ `With*` methods);
- Middleware pipeline: `IHarnessMiddleware` four hooks, 15+ built-in middlewares (workspace context / @path expansion / compaction / inbox / subagent / team / plan mode / transcript / memory maintenance, etc.);
- Workspace: `WorkspaceManager`, AGENTS.md / MEMORY.md / KNOWLEDGE.md injection, tools.json;
- Filesystem: `IFilesystem` (local / overlay / composite / sandbox / remote);
- Sandbox: `SandboxBase` four-branch lifecycle, `SandboxManager` leases, snapshot system;
- Memory: `SessionTranscriptWriter` / `SessionTree` / `MemoryFlushManager` / `MemoryConsolidator`;
- Compaction: `CompactionMiddleware` / `ConversationCompactor` / `ToolResultEviction`;
- Skills: `WorkspaceSkillRepository` / `SkillCatalog` / `SkillLoadTool` / Skill Curator;
- Subagent: `SubagentDeclaration` / `DefaultAgentManager` / remote protocol;
- Team: `ITeamClient` / `LocalTeamClient`;
- Gateway and Channel: `IGateway` / `IChannel` / `ChannelRouter` / `ChatUiChannel`.

**Extension Packages**

- Store: Redis / MySQL / PostgreSQL / OSS / COS (all implement `IAgentStateStore`);
- Vector: Elasticsearch / Milvus / PgVector / Qdrant (`IVectorStore`);
- Skill repository: Git / MySql / PostgreSql;
- Channels: DingTalk / Feishu / WeCom / GitHub / GitLab;
- Sandbox: Docker / E2B / Daytona / AgentRun / Kubernetes;
- Memory / RAG clients: Mem0 / ReMe / Bailian, Dify / RagFlow / Haystack / Bailian;
- Scheduler: Quartz / XxlJob;
- Nacos: registry discovery / prompt / skill;
- Observability: OpenTelemetry tracing middleware.

**Host Applications**

- `AgentScope.TUI` (Terminal.Gui terminal chat), `AgentScope.Uno` (Uno Platform cross-platform desktop).

## 1.x

The 1.x series was based on the old API (`ReActAgent.Builder()`, string model IDs, `RuntimeContext.Builder()`, fine-grained AgentEvent streams). See [Change Log](../change-log.md) for 2.0 migration guidance.
