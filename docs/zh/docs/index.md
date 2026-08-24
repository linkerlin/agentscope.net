---
title: "AgentScope 2.0 是什么？"
description: "Harness 工程化、无状态多会话、可插拔存储与协议层。"
---

AgentScope .NET 2.0 定位为**面向生产环境的智能体运行时**：核心推理引擎（`EnhancedReActAgent`）+ 工程化外壳（`HarnessAgent`）+ 可插拔的存储 / 渠道 / 协议扩展。

## 1 · Harness 工程化

`AgentScope.Harness` 把长期运行智能体所需的工程设施一次给齐：

- **工作区（Workspace）**：`AGENTS.md` / `MEMORY.md` / `KNOWLEDGE.md` / `skills/` / `subagents/` 全部以磁盘 Markdown 表达，每轮自动注入系统提示词；
- **中间件管道**：`IHarnessMiddleware` 洋葱模型（回合 / 模型调用 / 工具执行 / 系统提示词四个挂点），内置 15+ 中间件按 `Order` 排序执行；
- **上下文管理**：`CompactionMiddleware` 阈值标记 + `ConversationCompactor` 截断 / 修剪 / 摘要 + `ToolResultEviction` 大结果落盘；
- **记忆**：会话转录（JSONL）、`MemoryFlushManager` 刷写、`MemoryConsolidator` 定期整合；
- **子 Agent / 团队**：`SubagentDeclaration` 声明式子 Agent（支持远端）、`LocalTeamClient` 任务协作；
- **文件系统抽象**：`IFilesystem` 本地 / 叠加 / 组合 / 沙箱四类实现，`LocalFsMode` 三档隔离。

## 2 · 无状态多会话

`EnhancedReActAgent` 与 `HarnessAgent` 都是**无状态引擎**：

- 一个实例服务任意 `(UserId, SessionId)` 组合，每次调用通过 `RuntimeContext`（record + `With*` 派生）标识；
- 记忆实现 `IMemory` 可替换：`MemoryBase`（内存）、`SqliteMemory`（SQLite 落盘）、`StateBackedMemory`（自动写入 `IAgentStateStore`）；
- `SessionManager` + `IStateModule`（`SaveTo` / `LoadFrom` / `LoadIfExists`）支持会话级状态保存与恢复；
- 分布式状态：`AgentScope.Extensions.Store.*`（Redis / MySQL / PostgreSQL / OSS / COS）全部实现 `IAgentStateStore`。

## 3 · 可插拔扩展

| 扩展系列 | 接口 / 基类 | 说明 |
|----------|-------------|------|
| `Store.*` | `AgentScope.Core.State.IAgentStateStore` | 分布式状态存储 |
| `Vector.*`（Elasticsearch / Milvus / PgVector / Qdrant） | `AgentScope.Extensions.Vector.IVectorStore` | 向量检索 |
| `Skill.*`（Git / MySql / PostgreSql） | `AgentScope.Extensions.Skill.ISkillRepository` | 技能仓库后端 |
| `Channel.*`（DingTalk / Feishu / WeCom / GitHub / GitLab） | `AgentScope.Extensions.Channel.IChannel` | 即时通讯渠道 |
| `Sandbox.*`（Docker / E2B / Daytona / AgentRun / Kubernetes） | `AgentScope.Extensions.Sandbox.ISandbox` | 执行隔离环境 |
| `Mem.*`（Mem0 / ReMe / Bailian） | 独立 HTTP 客户端 | 托管长期记忆 |
| `Rag.*`（Dify / RagFlow / Haystack / Bailian） | 独立 HTTP 客户端 | 托管 RAG 服务 |
| `Scheduler.*`（Quartz / XxlJob） | `AgentScope.Extensions.Scheduler.IAgentScheduler` | 定时触发 Agent |
| `Tracing.OpenTelemetry` | `AgentScope.Harness.Middleware.IHarnessMiddleware` | OTLP 分布式追踪 |
| `Nacos*` | `AgentScope.Core.Service.Discovery.IAgentRegistry` | 服务注册发现 / 提示词 / 技能仓库 |

协议层（A2A / Agent Protocol / AgUI）与模型层（A2A 客户端、AgUI 适配器）内置于 `AgentScope.Core`。

## 迁移提示

v1 时代的 `ReActAgent`（及其 Builder）在 2.0 已标记 `[Obsolete]`；v1 的 `RuntimeContext.Builder()`、字符串模型 id、`StreamAsync` 等 API 不再使用。当前推荐的编码方式见 [快速上手](./quickstart.md)，完整变更见 [变更说明](./change-log.md)。
