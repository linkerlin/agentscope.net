---
title: "Release Notes"
description: "AgentScope .NET 版本发布记录"
---

## 2.0.1（当前）

首个 2.0 稳定版本。目标框架 `net10.0`。

**核心（AgentScope.Core）**

- `EnhancedReActAgent` 取代 `ReActAgent`（后者标记 `[Obsolete]`）；
- 模型体系：OpenAI / DashScope / Anthropic / Gemini / DeepSeek / Ollama / Mock 全部内置，统一 `IModel` + `IStreamingChatModel` 流式接口；
- 消息：`Msg.Builder()`、`ContentBlock` record 体系（文本 / 图片 / 音频 / 视频 / 工具调用 / 工具结果 / 思考块）；
- 事件：`Event` + `EventType` 粗粒度流式事件，`AgentEvent` 细粒度 record 层次保留给协议层；
- 工具：`[Tool]` / `[ToolParam]` 特性注册、`Toolkit` 分组、`ToolExecutor` 重试超时、内置文件 / Shell / 搜索 / 代码执行工具；
- MCP：`McpClientBuilder`（Stdio / Streamable HTTP / SSE）+ `McpManager` 工具发现；
- 权限：`PermissionEngine` 六步决策状态机 + HITL 确认回调；
- Hook：`HookManager` + 11 个生命周期回调；
- 状态：`IAgentStateStore`（InMemory / JsonFile）、`Session` / `SessionManager`、`IStateModule`（SaveTo / LoadFrom / LoadIfExists）、`StateBackedMemory`、`SqliteMemory`、`InMemoryLongTermMemory`；
- 结构化输出：`GenerateStructuredOutputAsync<T>`；
- 追踪：`AgentScope.Core.Tracing` Jsonl 导出；
- 协议：A2A 客户端 / 服务器、AgUI 适配器、Agent Protocol 任务客户端；
- 服务发现：`IAgentRegistry`（InMemory / Nacos 扩展）。

**Harness（AgentScope.Harness）**

- `HarnessAgent` + `HarnessAgentBuilder`（20+ `With*` 方法）；
- 中间件管道：`IHarnessMiddleware` 四挂点，内置 15+ 中间件（工作区上下文 / @path 展开 / 压缩 / 收件箱 / 子 Agent / 团队 / 计划模式 / 转录 / 记忆维护等）；
- 工作区：`WorkspaceManager`、AGENTS.md / MEMORY.md / KNOWLEDGE.md 注入、tools.json；
- 文件系统：`IFilesystem`（本地 / 叠加 / 组合 / 沙箱 / 远端）；
- 沙箱：`SandboxBase` 四分支生命周期、`SandboxManager` 租约、快照体系；
- 记忆：`SessionTranscriptWriter` / `SessionTree` / `MemoryFlushManager` / `MemoryConsolidator`；
- 压缩：`CompactionMiddleware` / `ConversationCompactor` / `ToolResultEviction`；
- 技能：`WorkspaceSkillRepository` / `SkillCatalog` / `SkillLoadTool` / 技能策展（Curator）；
- 子 Agent：`SubagentDeclaration` / `DefaultAgentManager` / 远端协议；
- 团队：`ITeamClient` / `LocalTeamClient`；
- 网关与渠道：`IGateway` / `IChannel` / `ChannelRouter` / `ChatUiChannel`。

**扩展包**

- 存储：Redis / MySQL / PostgreSQL / OSS / COS（均实现 `IAgentStateStore`）；
- 向量：Elasticsearch / Milvus / PgVector / Qdrant（`IVectorStore`）；
- 技能仓库：Git / MySql / PostgreSql；
- 渠道：钉钉 / 飞书 / 企业微信 / GitHub / GitLab；
- 沙箱：Docker / E2B / Daytona / AgentRun / Kubernetes；
- 记忆 / RAG 客户端：Mem0 / ReMe / Bailian、Dify / RagFlow / Haystack / Bailian；
- 调度：Quartz / XxlJob；
- Nacos：注册发现 / 提示词 / 技能；
- 可观测性：OpenTelemetry 追踪中间件。

**宿主应用**

- `AgentScope.TUI`（Terminal.Gui 终端聊天）、`AgentScope.Uno`（Uno Platform 跨平台桌面）。

## 1.x

1.x 系列基于旧 API（`ReActAgent.Builder()`、字符串模型 id、`RuntimeContext.Builder()`、细粒度 AgentEvent 流）。2.0 迁移指引见 [变更说明](./change-log.md)。
