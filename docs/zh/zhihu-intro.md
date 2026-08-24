# AgentScope.NET v2.0：C# 也能写智能体？是的，而且是企业级的

> **原仓库**（原作者 v1.1）：[github.com/linkerlin/agentscope.net](https://github.com/linkerlin/agentscope.net)

> **Fork（v2.0.1）**：[github.com/sky92archangel/agentscope.net](https://github.com/sky92archangel/agentscope.net)

> **在线文档**：[sky92archangel.github.io/agentscope.net](https://sky92archangel.github.io/agentscope.net/)

> **上游（Java 版）**：[github.com/agentscope-ai/agentscope-java](https://github.com/agentscope-ai/agentscope-java)

> **许可证**: Apache 2.0 | **版本**: v2.0.1 | **.NET**: net10.0


---

## 从 Java 到 C#：一次完整的 1:1 移植

[AgentScope](https://github.com/agentscope-ai/agentscope-java) 是阿里巴巴开源的多智能体框架，Java 版已经迭代到 v2.0.1。C# 版的起点来自 **linkerlin**，他把 Core 模块移植到了 v1.1。

**sky92archangel 对项目fork后做了什么？** 把 `AgentScope.Core` 从 v1.1 同步升级到了 v2.0.1——这不是简单的版本号递增，而是把 Java v2.0.1 的 22 个核心模块、42 个扩展项目全部"翻译"成了 C#。同时配套实现了 **AgentScope.Harness**，一个开箱即用的智能体运行时工程框架。

来，看看这个项目能做到什么。

---

## 支持的 LLM 供应商

| 供应商 | 模型类 | 说明 |
|--------|--------|------|
| OpenAI | `OpenAIModel` | GPT-4o / GPT-4 / GPT-3.5 |
| Azure OpenAI | `AzureOpenAIModel` | Azure 托管 OpenAI |
| Anthropic | `AnthropicModel` | Claude 3.5 / 4 系列 |
| Google Gemini | `GeminiModel` | Gemini Pro / Flash / Ultra |
| 阿里云 DashScope | `DashScopeModel` | 通义千问全系列 |
| DeepSeek | `DeepSeekModel` | deepseek-chat / deepseek-reasoner |
| Ollama | `OllamaModel` | 本地私有化部署 |
| Mock | `MockModel` | 测试用模拟模型 |

所有模型统一 `IModel` + `IStreamingChatModel` 双接口，全在 `AgentScope.Core` 包内，无需额外安装扩展包。

---

## 三行代码创建一个智能体

```csharp
using AgentScope.Core.Model;

IModel model = new DeepSeekModel("deepseek-chat", "sk-your-key");
// 或用任意 OpenAI 兼容端点：
// IModel model = new OpenAIModel("deepseek-chat", "none",
//     "http://localhost:11434/v1");

HarnessAgent agent = new HarnessAgentBuilder()
    .WithName("assistant")
    .WithModel(model)
    .WithWorkspaceRoot("./workspace")
    .Build();

Msg reply = await agent.CallAsync("你好！");
```

这就跑起来了一个带工作区、文件系统、上下文管理的智能体。加上流式输出：

```csharp
await foreach (var ev in agent.StreamEventsAsync("分析一下..."))
{
    if (ev.Type == EventType.ReasoningChunk)
        Console.Write(ev.Message?.GetTextContent());
}
```

模型思考的每一个 token 都能实时推送给前端。

---

## 项目规模：不只是"能用"

| 指标 | 数值 |
|------|------|
| C# 源文件 | 889 个 |
| 有效代码行 | ~66,000 行 |
| 核心模块 | 22/22 全部完成 |
| 扩展项目 | 42 个 |
| Core / Harness 构建 | 0 错误 |

**Core 模块（307 个 .cs）** 包含了完整的智能体生态：

| 模块 | 说明 |
|------|------|
| EnhancedReActAgent | 推理-行动循环引擎，支持 Hook、权限、流式、中断 |
| Model 层 | 7 种 LLM 提供商：OpenAI / Anthropic / Gemini / DashScope / DeepSeek / Ollama / Mock |
| Tool 系统 | `[Tool]` 特性自动注册、Toolkit 分组、MCP 协议（stdio/SSE/HTTP） |
| Memory 系统 | SqliteMemory / 长期记忆 / 状态持久化 |
| Message 系统 | Msg + ContentBlock 多模态消息（文本/图片/音视频/工具调用） |
| Skill 系统 | Markdown 技能仓库自动发现与加载 |
| A2A 协议 | 完整 Server + Client |
| Pipeline | 7 种节点编排 |
| Workflow | DAG 工作流引擎（Java 版没有） |
| RAG | 向量存储 + 知识检索 |
| MultiAgent | Group / Router / Coordinator |
| Session / State | 无状态多会话，支持持久化恢复 |

**Harness 工程框架（203 个 .cs）** 是把智能体从"能跑"变成"能上线"的关键：

| 组件 | 说明 |
|------|------|
| 中间件管道 | 15+ 内置中间件，洋葱模型，四挂点（Agent/模型调用/工具执行/系统提示词） |
| 工作区 | AGENTS.md / MEMORY.md / skills/ / subagents/ 自动注入 |
| 文件系统 | 本地 / 叠加 / 沙箱 / 组合，三档隔离级别 |
| Gateway 网关 | Agent 调用入口层，统一拦截与增强 |
| 消息总线 / Inbox | 总线 + 收件箱中间件，支持 Agent 间消息收发与异步工具注册 |
| 子 Agent / 团队 | 声明式子 Agent（支持远程）+ 进程内团队协作 |
| 上下文压缩 | 自动截断 / 摘要 + 大工具结果落盘 |
| 计划模式（PlanMode） | 计划驱动式执行中间件，支持分步执行 |
| 记忆管理 | 会话转录（JSONL）、MemoryFlushManager 刷写、MemoryConsolidator 定期整合 |
| 技能策展 | 自动发现工作区技能 + 使用统计 + 生命周期管理 |
| 追踪与转录 | AgentTraceMiddleware 调用链追踪 + TranscriptMiddleware 完整会话录制 |
| 隔离作用域 | Session / User / Agent 三级隔离粒度 |
| 协调层 | IPeriodicGate 周期性门控，支持持久化存储后端的定时触发 |
| 沙箱 | Docker / K8s / E2B / AgentRun / Daytona |

---

## 42 个扩展项目：随取随用

项目附带了 **42 个扩展工程**，涵盖了生产环境需要的各种集成：

- **存储**: Redis / MySQL / PostgreSQL / OSS / COS
- **向量**: Elasticsearch / Milvus / PgVector / Qdrant
- **渠道**: 钉钉 / 飞书 / 企业微信 / GitHub / GitLab
- **沙箱**: Docker / Kubernetes / E2B / Daytona / AgentRun
- **调度**: Quartz / XXL-JOB
- **RAG**: Dify / RagFlow / Haystack / 百炼
- **记忆**: Mem0 / ReMe / 百炼
- **可观测**: OpenTelemetry 分布式追踪
- **更多**: Nacos 服务发现 / Higress / Studio / Training

全部按接口编程——`IAgentStateStore`、`IVectorStore`、`ISandbox`、`IChannel`，扩展即插即用。

---

## 一手 Java 一手 C# 的开发者看过来

### 对标关系

| Java | C# |
|------|-----|
| agentscope-core | AgentScope.Core ✅ |
| agentscope-harness | AgentScope.Harness ✅ |
| agentscope-extensions | 42 个子项目 ✅ |

### C# 独有功能（Java 版没有）

- **Workflow 引擎**：DAG 工作流
- **A2A 完整协议栈**：同时包含 Server + Client
- **Pipeline 管道系统**：7 种节点编排
- **TUI 终端应用**：Terminal.Gui 交互界面
- **Uno 桌面应用**：跨平台 GUI
- **Docker 沙箱**：Java 版未内置
- **DeepSeek 模型**：原生支持
- **XXL-JOB 调度**：Quartz 之外的选择

### Java 版独有的（C# 暂未实现）

- agentscope-service（Spring Boot 微服务平台）
- 管理 REST API / JWT 认证 / SSE 事件流
- Cron 周期性调度 / JPA 持久化

本质差异一句话概括：
> **Java**: Core → Harness → Spring Boot 微服务  
> **C#**: Core → Harness → **你来写 ASP.NET Core 宿主**

C# 提供了完整的类库和运行时框架，但不捆绑任何 Web 宿主。你可以自由选择 ASP.NET Core、SignalR、gRPC 或者自己写控制台应用来承载智能体。

---

## 与微软 Agent Framework（MAF）的对比

2025 年 10 月，微软正式推出了 [Microsoft Agent Framework](https://learn.microsoft.com/zh-cn/agent-framework/overview/)（以下简称 MAF），它是 **Semantic Kernel 和 AutoGen 的直接继承者**——由同一团队打造，将 SK 的企业级能力与 AutoGen 的多智能体抽象合二为一，并新增了工作流引擎。SK 和 AutoGen 已进入维护模式，微软提供了完整的迁移指南。MAF 支持 C# / Python / Go，开源在 [github.com/microsoft/agent-framework](https://github.com/microsoft/agent-framework)。

有趣的是，MAF 也定义了一个 **Harness** 概念（`Microsoft.Agents.AI.Harness`），这与 AgentScope.Harness 英雄所见略同。但两者在设计哲学和实现深度上有显著差异：

| 维度 | 微软 MAF | AgentScope.NET |
|------|----------|----------------|
| 定位 | 统一 Agent SDK（继承 SK + AutoGen） | Java agentscope-java v2.0.1 的完整 C# 移植 + 独有增强 |
| Agent 创建 | `IChatClient.AsHarnessAgent()` 扩展方法 | `HarnessAgentBuilder` 流式 Builder 模式 |
| MultiAgent | 多 Agent 对话模式（顺序/并发/群聊/交接） | MultiAgent（Group/Router/Coordinator）+ 子 Agent 声明 + 团队协作 + 声明式子 Agent 文件 |
| 工作流 | 基于图的工作流引擎（有状态/检查点/嵌套） | DAG 工作流引擎（内置） |
| Harness 功能 | 15 项（todo/模式/压缩/内存/审批/OTel/网页搜索/技能/后台 Agent/循环等） | 15+ 中间件管道 + 工作区 + 文件系统 + 沙箱 + 上下文压缩 + 记忆管理 + 追踪/转录 + 隔离作用域 |
| 工具系统 | `AIFunctionFactory` + MCP 客户端 | `[Tool]` 特性注册 + Toolkit 分组 + MCP（3 种传输）+ SubAgentTool |
| 权限/审批 | `ApprovalRequiredAIFunction` + 工具审批中间件 | 三态决策引擎（Allow/Deny/Ask）+ HITL 确认回调 |
| 渠道集成 | 无内置，通过 Graph/Fabric 连接 | 内置 5 种渠道：钉钉 / 飞书 / 企微 / GitHub / GitLab |
| 沙箱 | 无 | Docker / K8s / E2B / Daytona / AgentRun |
| 调度 | 无 | Quartz / XXL-JOB |
| 状态存储 | 可插拔内存（Redis/Pinecone/Qdrant/ES/Postgres） | `IAgentStateStore`（6 种实现）+ Session 状态机 |
| 协议 | MCP + A2A + OpenAPI | MCP（3 种传输）+ A2A（Server+Client）+ AgUI |
| 提供商 | Azure OpenAI / OpenAI / Anthropic / Ollama / Foundry | 8 种（含 DeepSeek / DashScope / Gemini / Mock） |
| 可观测 | OpenTelemetry 内置 | AgentTrace + Transcript 双重 + OpenTelemetry 扩展 |
| 部署模式 | Azure AI Foundry 云端托管 | 无捆绑宿主，自由选择 ASP.NET Core / SignalR / gRPC / 控制台 |
| 许可证 | MIT | Apache 2.0 |
| 代码规模 | 数万行 | ~66,000 行，889 个 .cs 文件，42 个扩展项目 |

**核心差异一句话**：MAF 是微软官方"大一统"的 Agent SDK，深度绑定 Azure AI Foundry 生态；AgentScope.NET 是完全开源、独立于任何云平台的 Java agentscope 1:1 移植实现，更适合需要多云部署、深度定制或已有 Java agentscope 迁移需求的团队。

### Harness 概念对比

两者都叫 Harness，但内涵不同：

| | MAF Harness | AgentScope.Harness |
|--|-------------|-------------------|
| 实现方式 | `IChatClient` 扩展方法 `AsHarnessAgent()` | `HarnessAgentBuilder` 完整 Builder 模式 |
| 功能开关 | `DisableXxx` 选项标记 | 中间件按需装配 |
| 默认能力 | todo / 模式 / 文件记忆 / 压缩 / OTel / 网页搜索 | 中间件管道 / 工作区 / 文件系统 / 子 Agent / 团队 / 压缩 / 记忆 / 追踪 |
| 自定义 | `HarnessAgentOptions` + `AIContextProviders` | 自定义 `IHarnessMiddleware` + 洋葱模型管道 |

### 说回 SK 和 AutoGen

Semantic Kernel 和 AutoGen 是 MAF 的前身，目前已进入维护模式，微软不再建议新项目使用。如果你在社区看到 SK / AutoGen 的教程，可以了解其设计思想，但新项目建议直接上 MAF。

---

## 文档站

项目配备了完整的中英文文档站，共 164 篇文档：

```bash
cd docs
npx -y http-server . -p 3001 -c-1
# 打开 http://localhost:3001
```

侧边栏导航、Markdown 实时渲染、内链点击切换，开箱即用。

---

## 写在最后

这个项目是 Java agentscope-java v2.0.1 的 C# 移植实现，包含了全部核心模块和扩展项目。如果你在 .NET 技术栈里做 AI 应用开发，或者想从 Python/LangChain "叛逃"到强类型的 C# 世界，这个项目应该能给你一个足够完整的起点。

仓库地址：[github.com/agentscope-ai/agentscope.net](https://github.com/agentscope-ai/agentscope.net)

欢迎 Star、Issue、PR。框架还有不少可以打磨的地方，一起玩。
