# 集成总览

本节汇总 AgentScope .NET 与第三方系统、生态服务的集成扩展。每个扩展都是独立的 NuGet 包（`AgentScope.Extensions.*`），按需引入。

## 模型提供商

所有模型（OpenAI / DashScope / Anthropic / Gemini / DeepSeek / Ollama / Mock）都**内置在 `AgentScope.Core`**，无需扩展包。构造方式与流式接口见[模型文档](../docs/building-blocks/model.md)。

| 提供商 | 模型类 | 环境变量 | 说明 |
|--------|--------|----------|------|
| OpenAI | `OpenAIModel` | `OPENAI_API_KEY` | 也覆盖一切 OpenAI 兼容端点（vLLM、OneAPI 等） |
| DashScope（通义千问） | `DashScopeModel` | `DASHSCOPE_API_KEY` | 默认 `https://dashscope.aliyuncs.com` |
| Anthropic（Claude） | `AnthropicModel` | `ANTHROPIC_API_KEY` | 默认 `https://api.anthropic.com` |
| Gemini | `GeminiModel` | `GEMINI_API_KEY` | 默认 `gemini-pro` |
| DeepSeek | `DeepSeekModel` | `DEEPSEEK_API_KEY` | 默认 `deepseek-chat` |
| Ollama（本地） | `OllamaModel` | `OLLAMA_BASE_URL`（可选） | 默认本机 11434 |
| Mock（测试） | `MockModel` | — | 回显，不发网络请求 |
| GLM / Kimi / MiniMax 等 | `OpenAIModel` + `baseUrl` | 对应厂商 Key | 经 OpenAI 兼容端点接入，见[GLM](model/glm.md) / [Kimi](model/kimi.md) / [MiniMax](model/minimax.md) |

## 分布式存储（Distributed Store）

生产多副本部署所需的状态存储，全部实现 `AgentScope.Core.State.IAgentStateStore`：

| 后端 | 包 | 状态存储类（包装 `*DistributedStore`） |
|------|-----|----------------------------------------|
| Redis | `AgentScope.Extensions.Store.Redis` | `RedisAgentStateStore`（便捷构造 `(connectionString, keyPrefix)`） |
| MySQL | `AgentScope.Extensions.Store.MySql` | `MySqlAgentStateStore` |
| PostgreSQL | `AgentScope.Extensions.Store.PostgreSql` | `PostgreSqlAgentStateStore` |
| 阿里云 OSS | `AgentScope.Extensions.Store.Oss` | `OssAgentStateStore` |
| 腾讯云 COS | `AgentScope.Extensions.Store.Cos` | `CosAgentStateStore` |

- [分布式存储总览](distributed/index.md)
- [Redis](distributed/redis.md) · [MySQL](distributed/mysql.md) · [OSS](distributed/oss.md)

## 沙箱执行环境（Sandbox）

实现 `AgentScope.Extensions.Sandbox.ISandbox` 的隔离执行环境，经适配接入 Harness 沙箱体系（见[沙箱](../docs/harness/sandbox.md)）：

- Docker — `AgentScope.Extensions.Sandbox.Docker`（`DockerSandbox(image, containerName?)`）
- Kubernetes — `AgentScope.Extensions.Sandbox.Kubernetes`
- E2B — `AgentScope.Extensions.Sandbox.E2B`
- Daytona — `AgentScope.Extensions.Sandbox.Daytona`
- AgentRun（阿里云） — `AgentScope.Extensions.Sandbox.AgentRun`

## 记忆（Memory）

托管长期记忆的 HTTP 客户端（独立类，需自行适配 Core `ILongTermMemory` 或 `LongTermMemoryTools`）：

- [Mem0](memory/mem0.md) — `Mem0LongTermMemory`
- [ReMe](memory/reme.md) — `ReMeLongTermMemory`
- [百炼记忆](memory/bailian.md) — `BailianLongTermMemory`

## RAG 知识库

托管 RAG 服务的 HTTP 客户端（独立类）：

- [Dify](rag/dify.md) — `DifyRagClient`
- [RAGFlow](rag/ragflow.md) — `RagFlowRagClient`
- [Haystack](rag/haystack.md) — `HaystackRagClient`
- [百炼知识库](rag/bailian.md) — `BailianRagClient`
- [Simple（本地 RAG）](rag/simple.md) — 基于 `AgentScope.Core.RAG`（`IKnowledge` / `VectorStore`）

## 技能仓库（Skill）

实现 `AgentScope.Extensions.Skill.ISkillRepository`：

- [Git 技能仓库](skill/git-repository.md) — `GitSkillRepository`
- [MySQL 技能仓库](skill/mysql-repository.md) — `MySqlSkillRepository`
- [PostgreSQL 技能仓库](skill/postgresql-repository.md) — `PostgreSqlSkillRepository`
- [Nacos 技能仓库](infrastructure/nacos.md) — `NacosSkillRepository`

## Channel 适配器

实现 `AgentScope.Extensions.Channel.IChannel`（webhook 客户端风格，与 Harness 内部 `IChannel` 平行，接入需适配，见[Channel 文档](../docs/harness/channel.md)）：

- [钉钉](channel/dingtalk.md) · [飞书 / Lark](channel/feishu.md) · [企业微信](channel/wecom.md) · [GitHub](channel/github.md) · [GitLab](channel/gitlab.md)

## 智能体协议

`AgentScope.Core` 内置协议层：

- [A2A（Agent-to-Agent）](protocol/a2a.md) — `A2aAgent` / `AgentScopeA2aServer`
- [AG-UI](protocol/agui.md) — `AguiAgentAdapter` / `AguiAgentRegistry`
- [Agent Protocol](protocol/agent-protocol.md) — `AgentProtocolTaskClient`

## 基础设施 / 中间件

- [Higress AI 网关](infrastructure/higress.md) — `HigressMcpClient` / `HigressToolkit`
- [Nacos](infrastructure/nacos.md) — 注册发现 / 提示词 / 技能仓库
- [Scheduler（Quartz / XXL-Job）](infrastructure/scheduler.md) — `QuartzAgentScheduler` / `XxlJobAgentScheduler`

## 可观测性与生态

- [OpenTelemetry](../docs/others/going-to-production.md#可观测性) — `AgentScope.Tracing.OpenTelemetry`（`AddAgentScopeTracing`）
- [AgentScope Studio](ecosystem/studio.md) — `AgentScopeStudioClient`
- [在线训练（Training）](ecosystem/training.md) — `TrainingManager`
- [Chat Completions Web](ecosystem/chat-completions-web.md)

## 文档解析

- PDF：`AgentScope.Extensions.Document.Pdf.PdfReader`（UglyToad.PdfPig）
- Word：`AgentScope.Extensions.Document.Word.WordReader`（OpenXML）

两者均继承 `AbstractChunkingReader`：`(int chunkSize = 1000, SplitStrategy strategy = SplitStrategy.Paragraph, int overlap = 200)`。
