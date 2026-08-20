# Integration Overview

This section collects the AgentScope .NET extensions that connect to third-party systems and ecosystem services. Each extension is an independent NuGet package under `AgentScope.Extensions.*` — pull in only what you need.

## Model Providers

All models (OpenAI / DashScope / Anthropic / Gemini / DeepSeek / Ollama / Mock) are **built into `AgentScope.Core`** — no model extension packages exist. See [Model](../docs/building-blocks/model.md) for constructor signatures and the streaming interface.

| Provider | Model class | Environment variable | Notes |
|----------|-------------|----------------------|-------|
| OpenAI | `OpenAIModel` | `OPENAI_API_KEY` | Also covers any OpenAI-compatible endpoint (vLLM, OneAPI, etc.) |
| DashScope (Qwen) | `DashScopeModel` | `DASHSCOPE_API_KEY` | Default `https://dashscope.aliyuncs.com` |
| Anthropic (Claude) | `AnthropicModel` | `ANTHROPIC_API_KEY` | Default `https://api.anthropic.com` |
| Gemini | `GeminiModel` | `GEMINI_API_KEY` | Default `gemini-pro` |
| DeepSeek | `DeepSeekModel` | `DEEPSEEK_API_KEY` | Default `deepseek-chat` |
| Ollama (local) | `OllamaModel` | `OLLAMA_BASE_URL` (optional) | Default local 11434 |
| Mock (testing) | `MockModel` | — | Echoes input, no network |
| GLM / Kimi / MiniMax etc. | `OpenAIModel` + `baseUrl` | provider key | Via OpenAI-compatible endpoints, see [GLM](model/glm.md) / [Kimi](model/kimi.md) / [MiniMax](model/minimax.md) |

## Distributed Storage (Distributed Store)

State stores for multi-replica production deployments. All implement `AgentScope.Core.State.IAgentStateStore`:

| Backend | Package | State store class (wraps `*DistributedStore`) |
|---------|---------|------------------------------------------------|
| Redis | `AgentScope.Extensions.Store.Redis` | `RedisAgentStateStore` (convenience ctor `(connectionString, keyPrefix)`) |
| MySQL | `AgentScope.Extensions.Store.MySql` | `MySqlAgentStateStore` |
| PostgreSQL | `AgentScope.Extensions.Store.PostgreSql` | `PostgreSqlAgentStateStore` |
| Alibaba OSS | `AgentScope.Extensions.Store.Oss` | `OssAgentStateStore` |
| Tencent COS | `AgentScope.Extensions.Store.Cos` | `CosAgentStateStore` |

- [Distributed Storage Overview](distributed/index.md)
- [Redis](distributed/redis.md) · [MySQL](distributed/mysql.md) · [OSS](distributed/oss.md)

## Sandbox Execution Environments

Isolated execution environments implementing `AgentScope.Extensions.Sandbox.ISandbox`, adapted into the Harness sandbox layer (see [Sandbox](../docs/harness/sandbox.md)):

- Docker — `AgentScope.Extensions.Sandbox.Docker` (`DockerSandbox(image, containerName?)`)
- Kubernetes — `AgentScope.Extensions.Sandbox.Kubernetes`
- E2B — `AgentScope.Extensions.Sandbox.E2B`
- Daytona — `AgentScope.Extensions.Sandbox.Daytona`
- AgentRun (Alibaba) — `AgentScope.Extensions.Sandbox.AgentRun`

## Memory

Hosted long-term memory HTTP clients (standalone classes; adapt them to the Core `ILongTermMemory` interface or `LongTermMemoryTools` yourself):

- [Mem0](memory/mem0.md) — `Mem0LongTermMemory`
- [ReMe](memory/reme.md) — `ReMeLongTermMemory`
- [Bailian Memory](memory/bailian.md) — `BailianLongTermMemory`

## RAG Knowledge Bases

Hosted RAG service HTTP clients (standalone classes):

- [Dify](rag/dify.md) — `DifyRagClient`
- [RAGFlow](rag/ragflow.md) — `RagFlowRagClient`
- [Haystack](rag/haystack.md) — `HaystackRagClient`
- [Bailian Knowledge](rag/bailian.md) — `BailianRagClient`
- [Simple (local RAG)](rag/simple.md) — based on `AgentScope.Core.RAG` (`IKnowledge` / `VectorStore`)

## Skill Repositories

Implementing `AgentScope.Extensions.Skill.ISkillRepository`:

- [Git Skill Repository](skill/git-repository.md) — `GitSkillRepository`
- [MySQL Skill Repository](skill/mysql-repository.md) — `MySqlSkillRepository`
- [PostgreSQL Skill Repository](skill/postgresql-repository.md) — `PostgreSqlSkillRepository`
- [Nacos Skill Repository](infrastructure/nacos.md) — `NacosSkillRepository`

## Channel Adapters

Implementing `AgentScope.Extensions.Channel.IChannel` (webhook-client style, parallel to the Harness-internal `IChannel`; an adapter is required to wire them in — see [Channel](../docs/harness/channel.md)):

- [DingTalk](channel/dingtalk.md) · [Feishu / Lark](channel/feishu.md) · [WeCom](channel/wecom.md) · [GitHub](channel/github.md) · [GitLab](channel/gitlab.md)

## Agent Protocols

`AgentScope.Core` ships protocol support:

- [A2A (Agent-to-Agent)](protocol/a2a.md) — `A2aAgent` / `AgentScopeA2aServer`
- [AG-UI](protocol/agui.md) — `AguiAgentAdapter` / `AguiAgentRegistry`
- [Agent Protocol](protocol/agent-protocol.md) — `AgentProtocolTaskClient`

## Infrastructure

- [Higress AI Gateway](infrastructure/higress.md) — `HigressMcpClient` / `HigressToolkit`
- [Nacos](infrastructure/nacos.md) — registry / prompts / skill repository
- [Scheduler (Quartz / XXL-Job)](infrastructure/scheduler.md) — `QuartzAgentScheduler` / `XxlJobAgentScheduler`

## Observability & Ecosystem

- [OpenTelemetry](../docs/others/going-to-production.md#observability) — `AgentScope.Tracing.OpenTelemetry` (`AddAgentScopeTracing`)
- [AgentScope Studio](ecosystem/studio.md) — `AgentScopeStudioClient`
- [Online Training](ecosystem/training.md) — `TrainingManager`
- [Chat Completions Web](ecosystem/chat-completions-web.md)

## Document Parsing

- PDF: `AgentScope.Extensions.Document.Pdf.PdfReader` (UglyToad.PdfPig)
- Word: `AgentScope.Extensions.Document.Word.WordReader` (OpenXML)

Both inherit `AbstractChunkingReader`: `(int chunkSize = 1000, SplitStrategy strategy = SplitStrategy.Paragraph, int overlap = 200)`.
