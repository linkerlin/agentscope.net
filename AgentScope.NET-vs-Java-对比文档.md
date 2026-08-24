# AgentScope.NET vs AgentScope-Java 工程对比文档

## 1. 项目总览

| 维度 | AgentScope.NET | AgentScope-Java |
|------|---------------|-----------------|
| **语言/平台** | C# / .NET 10 (`net10.0`) | Java 17+ / Maven |
| **框架风格** | 纯类库 + 扩展 + 控制台/GUI应用 | 纯类库 + 扩展 + Spring Boot微服务 |
| **项目数量** | 46 个项目 (42 类库 + 2 应用 + 2 聚合) | ~30 个模块 (含示例和分发) |
| **构建系统** | dotnet / NuGet | Maven |
| **包管理** | NuGet (.nupkg) | Maven Central |
| **许可证** | Apache 2.0 | Apache 2.0 |
| **开源方** | 阿里巴巴 | 阿里巴巴 |
| **API风格** | C# 接口/委托/async/await | Java 接口/CompletableFuture/Reactor |

---

## 2. 模块映射关系

| Java 模块 | C# 对应工程 | 对标程度 | 说明 |
|-----------|-----------|---------|------|
| `agentscope-core` | `AgentScope.Core` | **完全对标** | 核心抽象：Agent、Model、Message、Tool、Skill、Memory、Hook等 |
| `agentscope-harness` | `AgentScope.Harness` | **完全对标** | 运行时引擎：中间件管道、Gateway网关、沙箱、团队、工作区 |
| `agentscope-extensions` | `AgentScope.Extensions` + 扩展子项目 | **完全对标** | 扩展SPI + 各供应商实现 |
| `agentscope-examples` | `examples/` 目录 | **部分对标** | 示例代码位置不同 |
| `agentscope-spring-boot-starters` | 无 | **Java独有** | Spring Boot自动配置 |

### 2.1 Core 层详细对照

| 核心概念 | Java (agentscope-core) | C# (AgentScope.Core) |
|---------|----------------------|---------------------|
| Agent接口 | `Agent`, `AgentBase`, `CallableAgent` | `IAgent`, `AgentBase`, `ICallableAgent` |
| 增强Agent | `ReActAgent` | `ReActAgent`, `EnhancedReActAgent` |
| 模型接口 | `Model`, `ChatModelBase` | `IModel`, `ModelBase`, `ChatModelBase` |
| 模型SPI | `ModelProvider` | `IModelProvider` |
| 消息模型 | `Msg`, `UserMessage`, `AssistantMessage` | `Msg`, `UserMessage`, `AssistantMessage` |
| 消息内容块 | `ContentBlock`, `TextBlock`, `ImageBlock`, `ThinkingBlock` | `ContentBlock`, `DataBlock` |
| 工具框架 | `Tool`, `ToolBase`, `ToolRegistry` | `ITool`, `ToolBase`, `ToolRegistry` |
| 技能系统 | `SkillBox`, `SkillRegistry` | `SkillBox`, `SkillRegistry` |
| 记忆系统 | `Memory`, `InMemoryMemory`, `LongTermMemory` | `IMemory`, `MemoryBase`, `LongTermMemory` |
| 状态管理 | `AgentStateStore`, `InMemoryAgentStateStore` | `IAgentStateStore`, `InMemoryAgentStateStore` |
| Hook系统 | `Hook` 接口 + 15事件 | `IHook` 接口 + 9阶段回调 |
| 中间件链 | `MiddlewareBase`, `MiddlewareChain` | `MiddlewareBase`, `MiddlewareChain` |
| 事件模型 | 35个Agent生命周期事件类 | `Event.cs` + 事件枚举 |
| RAG | `Knowledge`, `GenericRAGHook` | `IKnowledge`, `RAGHook` |
| 权限引擎 | `PermissionEngine` | `PermissionEngine` |
| 追踪 | `Tracer`, `OtelTracingMiddleware` | `ITracer`, `Tracer` |
| 格式化器 | `Formatter`, `JsonSchema` | `IFormatter`, `JsonSchemaUtils` |
| 凭据 | `CredentialBase`, `DeepSeekCredential` | `CredentialBase`, `DeepSeekCredential` |
| 中断控制 | `InterruptControl` | `InterruptibleAgentBase` |
| 优雅关闭 | `GracefulShutdownManager` | `GracefulShutdownManager` |
| 模型供应商实现 | OpenAI/DashScope/Gemini/Anthropic/Ollama | OpenAI/DashScope/Gemini/Anthropic/Ollama/DeepSeek |
| 工作区 | `workspace` 包 | `AgentScope.Core` 中无直接对应(在Harness中) |

### 2.2 Harness 层详细对照

| 概念 | Java (agentscope-harness) | C# (AgentScope.Harness) |
|------|--------------------------|------------------------|
| 主Agent | `HarnessAgent` | `HarnessAgent` |
| 构建器 | `HarnessAgentBuilderSupport` | `HarnessAgentBuilder` |
| Gateway | `Gateway` 接口 | `IGateway`, `HarnessGateway` |
| Subagent注册表 | `SubagentRegistry` | `SubagentRegistry` |
| 沙箱 | `Sandbox` 接口, `AbstractBaseSandbox` | `SandboxBase`, `ISandbox` |
| 文件系统 | `AbstractFilesystem`, `OverlayFilesystem` | `IFilesystem`, `OverlayFilesystem` |
| 团队 | `TeamClient`, `LocalTeamClient` | `ITeamClient`, `LocalTeamClient` |
| 中间件 | 20个中间件 | 20+中间件 |
| 内置工具 | 17个工具 | 16个工具 |
| 消息总线 | `MessageBus` | `IMessageBus`, `WorkspaceMessageBus` |
| 协调 | `PeriodicGate` | `IPeriodicGate` |
| 工作区管理 | `WorkspaceManager` | `WorkspaceManager` |
| 记忆管理 | `MemoryConsolidator`, `MemoryFlushManager` | `MemoryConsolidator`, `MemoryFlushManager` |
| Session转录 | `TranscriptStore` | `ITranscriptStore` |
| 隔离作用域 | `IsolationScope` | `IsolationScope` |

### 2.3 扩展层详细对照

| 扩展类别 | Java 模块 | C# 模块 |
|---------|----------|--------|
| OpenAI | `agentscope-extensions-model-openai` | `AgentScope.Core.Model.OpenAI`(内建) |
| DashScope | `agentscope-extensions-model-dashscope` | `AgentScope.Core.Model.DashScope`(内建) |
| Gemini | `agentscope-extensions-model-gemini` | `AgentScope.Core.Model.Gemini`(内建) |
| Anthropic | `agentscope-extensions-model-anthropic` | `AgentScope.Core.Model.Anthropic`(内建) |
| Ollama | `agentscope-extensions-model-ollama` | `AgentScope.Core.Model.Ollama`(内建) |
| 钉钉 | `agentscope-extensions-channel-dingtalk` | `AgentScope.Extensions.Channel.DingTalk` |
| 飞书 | `agentscope-extensions-channel-feishu` | `AgentScope.Extensions.Channel.Feishu` |
| 企业微信 | `agentscope-extensions-channel-wecom` | `AgentScope.Extensions.Channel.WeCom` |
| GitHub | `agentscope-extensions-channel-github` | `AgentScope.Extensions.Channel.GitHub` |
| GitLab | `agentscope-extensions-channel-gitlab` | `AgentScope.Extensions.Channel.GitLab` |
| Mem0 | `agentscope-extensions-mem0` | `AgentScope.Extensions.Mem.Mem0` |
| ReMe | `agentscope-extensions-reme` | `AgentScope.Extensions.Mem.ReMe` |
| 百炼记忆 | `agentscope-extensions-memory-bailian` | `AgentScope.Extensions.Mem.Bailian` |
| 百炼RAG | `agentscope-extensions-rag-bailian` | `AgentScope.Extensions.Rag.Bailian` |
| Dify | `agentscope-extensions-rag-dify` | `AgentScope.Extensions.Rag.Dify` |
| Haystack | `agentscope-extensions-rag-haystack` | `AgentScope.Extensions.Rag.Haystack` |
| RagFlow | `agentscope-extensions-rag-ragflow` | `AgentScope.Extensions.Rag.RagFlow` |
| E2B沙箱 | `agentscope-extensions-sandbox-e2b` | `AgentScope.Extensions.Sandbox.E2B` |
| Daytona沙箱 | `agentscope-extensions-sandbox-daytona` | `AgentScope.Extensions.Sandbox.Daytona` |
| AgentRun沙箱 | `agentscope-extensions-sandbox-agentrun` | `AgentScope.Extensions.Sandbox.AgentRun` |
| K8s沙箱 | `agentscope-extensions-sandbox-kubernetes` | `AgentScope.Extensions.Sandbox.Kubernetes` |
| Docker沙箱 | 无 | `AgentScope.Extensions.Sandbox.Docker` (C#独有) |
| Higress | `agentscope-extensions-higress` | `AgentScope.Extensions.Higress` |
| Nacos | `agentscope-extensions-nacos` | `AgentScope.Extensions.Nacos` |
| Aistio | `agentscope-extensions-aistio` | `AgentScope.Extensions.Aistio` |
| Studio | `agentscope-extensions-studio` | `AgentScope.Extensions.Studio` |
| 阿里OSS | `agentscope-extensions-oss` | `AgentScope.Extensions.Store.Oss` |
| 腾讯COS | `agentscope-extensions-cos` | `AgentScope.Extensions.Store.Cos` |
| Redis | `agentscope-extensions-redis` | `AgentScope.Extensions.Store.Redis` |
| MySQL | `agentscope-extensions-mysql` | `AgentScope.Extensions.Store.MySql` |
| PostgreSQL | `agentscope-extensions-postgresql` | `AgentScope.Extensions.Store.PostgreSql` |
| 训练 | `agentscope-extensions-training` | `AgentScope.Extensions.Training` |
| 调度 | `agentscope-extensions-scheduler`(Quartz) | `AgentScope.Extensions.Scheduler.Quartz` + `XxlJob` |

---

## 3. agentscope-service (Java) 对标分析

### 3.1 核心结论

**`agentscope-service`（Java）是一个完整的 Spring Boot 4 微服务部署平台，而 C# 项目中没有与之直接对应的 Web 宿主项目。**

Java `agentscope-service` 采用 **"四平面架构"**，构建在 `agentscope-harness` 之上：

| 平面 | 模块 | 端口 | 职责 |
|------|------|------|------|
| 网关平面 | `service-gateway` | 8080 | Spring Cloud Gateway 路由 |
| 数据平面 | `service-dataplane` | 8082 | HarnessAgent 回合执行、SSE流、HITL |
| 调度平面 | `service-scheduler` | 8083 | IM通道适配器、Cron部署、Hands Workers |
| 共享契约 | `service-common` | - | JPA实体、DTO、JWT认证、协调存储 |

### 3.2 C# 中对应功能分布

Java `agentscope-service` 的功能在 C# 中分散在多个项目中：

| Java service 功能 | C# 对应位置 | 说明 |
|------------------|------------|------|
| **IService 抽象** | `AgentScope.Core.Service/IService.cs` | 定义 `IService` 接口(继承IAgent)，含启动/停止/健康检查 |
| **ServiceBase 实现** | `AgentScope.Core.Service/ServiceBase.cs` | 抽象基类，心跳循环(30s)、状态管理 |
| **ServiceManager** | `AgentScope.Core.Service/ServiceManager.cs` | 服务注册/注销/启动/停止/健康检查 |
| **服务发现** | `AgentScope.Core.Service/IServiceDiscovery`, `InMemoryServiceDiscovery` | 服务发现接口+内存实现 |
| **控制面/数据面架构** | `AgentScope.Core.Service.Discovery/` | ControlPlaneService, DataPlaneRegistry, IAgentRegistry |
| **分布式后端配置** | `AgentScope.Core.Service.Discovery/DistributedBackend.cs` | `CreateInMemory` / `CreateWithNacos` 工厂 |
| **Channel路由** | `AgentScope.Harness.Gateway.Channel/` | ChannelRouter (8层路由), ChannelManager |
| **Gateway网关** | `AgentScope.Harness.Gateway/HarnessGateway.cs` | IGateway 接口 + HarnessGateway 实现 |
| **IM通道实现** | `AgentScope.Extensions.Channel.*` | 与Java完全对标: 钉钉/飞书/企微/GitHub/GitLab |
| **JWT认证** | 无 | C#无内建认证方案，需宿主自行实现 |
| **JPA持久化** | 无 | C#无JPA-like抽象，仅SQLite记忆 |
| **Spring Cloud Gateway** | 无 | C#无API网关组件 |
| **Cron部署调度** | 无 | C#无周期性任务调度 |
| **Hands Workers** | 无 | C#无独立Worker进程概念 |
| **SSE事件流** | 无(部分在A2A) | C#无SSE端点 |
| **管理API(REST)** | 无 | C#无REST API端点 |

### 3.3 架构差异本质

```
Java agentscope-service 架构:
┌─ User ──────────────────────────────────────────────┐
│  Frontend (React/Vite) ──→ service-gateway (8080)   │
│                               │                      │
│                    ┌──────────┼──────────┐           │
│                    ▼          ▼          ▼          │
│              service-   service-    service-        │
│              dataplane  scheduler   [control plane] │
│              (8082)     (8083)                      │
│                    │          │                      │
│                    └──────────┘                      │
│                          │                           │
│                    Shared Database                    │
│              (MySQL/PostgreSQL via JPA)              │
└──────────────────────────────────────────────────────┘

C# AgentScope.NET 架构 (当前):
┌─ User ──────────────────────────────────────────────┐
│  TUI / Uno GUI                                      │
│       │                                              │
│       ▼                                              │
│  AgentScope.Harness (单体进程内引擎)                  │
│       │                                              │
│       ▼                                              │
│  AgentScope.Core (核心抽象层)                          │
│       │                                              │
│       ▼                                              │
│  AgentScope.Extensions (扩展)                         │
└──────────────────────────────────────────────────────┘
```

**核心差异**：Java 版本将 Harness 引擎包装在 Spring Boot 微服务中，形成了可部署的生产平台；C# 版本仅提供类库，没有内建的微服务包装层。C# 的 `AgentScope.Core.Service` 只是一个轻量级服务抽象，不是部署平台。

---

## 4. 独有功能对比

### 4.1 C# 独有功能

| 功能 | 位置 | 说明 |
|------|------|------|
| **A2A完整协议栈** | `AgentScope.Core/A2A/` | 同时包含 Server(AgentScopeA2aServer + JSON-RPC) 和 Client(A2aAgent) |
| **Pipeline管道系统** | `AgentScope.Core/Pipeline/` | IPipelineNode + Pipeline + Nodes 编排引擎 |
| **Workflow工作流** | `AgentScope.Core/Workflow/` | IWorkflow + WorkflowEngine |
| **TUI终端应用** | `AgentScope.TUI/` | Terminal.Gui 构建的交互式终端界面 |
| **Uno桌面应用** | `AgentScope.Uno/` | Uno.WinUI 跨平台桌面GUI |
| **Docker沙箱** | `AgentScope.Extensions.Sandbox.Docker/` | Java版没有Docker沙箱 |
| **XXL-JOB调度** | `AgentScope.Extensions.Scheduler.XxlJob/` | Java版仅有Quartz |
| **向量存储实现** | `AgentScope.Extensions.Vector.*` | Elasticsearch/Milvus/PgVector/Qdrant |
| **OpenTelemetry追踪** | `AgentScope.Tracing.OpenTelemetry/` | 独立追踪项目 |
| **McpClient多协议** | `AgentScope.Core/MCP/` | SSE/Stdio/StreamableHttp 三种传输协议 |

### 4.2 Java 独有功能

| 功能 | 位置 | 说明 |
|------|------|------|
| **agentscope-service微服务平台** | `agentscope-service/` | Spring Boot 4 微服务部署平台 |
| **Spring Boot Starters** | `agentscope-spring-boot-starters/` | 11个自动配置Starter |
| **BOM/Distribution** | `agentscope-dependencies-bom/`, `agentscope-distribution/` | 依赖管理/分发 |
| **Hands Workers** | `service-scheduler/worker/` | 独立Worker进程执行工具 |
| **Cron部署系统** | `service-scheduler/` | 周期性Agent部署调度 |
| **JWT认证/授权** | `service-common/web/auth/` | 完整JWT认证 + ACL |
| **JPA持久化** | `service-common/web/persistence/jpa/` | 43个JPA实体 |
| **SSE事件流** | `service-dataplane/` | SSE实时流推送 |
| **管理REST API** | `service-dataplane/web/api/` | 完整的会话管理API |
| **React前端** | `agentscope-service/frontend/` | Vite + React管理界面 |

---

## 5. 代码实现风格差异

| 方面 | C# | Java |
|------|----|------|
| **异步模型** | `async/await` + `Task<T>` | `CompletableFuture` + Reactor `Flux`/`Mono` |
| **空安全** | `nullable enable` + `?` 注解 | `Optional`（部分） |
| **配置** | `DotNetEnv` | Spring `@ConfigurationProperties` |
| **DI容器** | 无内建 | Spring DI |
| **序列化** | `System.Text.Json` | Jackson |
| **ORM** | EF Core SQLite (仅记忆) | Spring Data JPA (完整) |
| **HTTP客户端** | `HttpClient` | OkHttp + WebClient |
| **命名风格** | `I`前缀接口, PascalCase | 无前缀, camelCase |
| **注释语言** | 中文 | 英文 |

---

## 6. 对比总结

### 架构层面
- **共同核心**：两套代码的 `core` + `harness` 层在概念和功能上高度一致，是同一架构的跨语言实现
- **主要差异**：Java 版本提供了完整的 Spring Boot 微服务部署平台 (`agentscope-service`)，而 C# 版本只提供类库，没有 Web 宿主工程

### agentscope-service 的 C# 对标结论

| 问题 | 答案 |
|------|------|
| Java `agentscope.service` 对标C#哪个工程？ | **没有直接对标的完整工程**。其功能分散在 `AgentScope.Core.Service` + `AgentScope.Harness` + `AgentScope.Extensions.Channel.*` |
| C#是否有等价的微服务平台？ | **没有**。C#仅提供 `IService` 抽象和 `Discovery` 服务发现基础设施 |
| C#如何部署Agent？ | 需要用户自行编写 ASP.NET Core 宿主程序，引用 `AgentScope.Harness` 和渠道扩展 |
| 缺失哪些关键组件？ | API网关、JPA持久化、JWT认证、管理REST API、SSE事件流、Cron调度、Frontend UI |

### 建议
如果需要将 C# 版本提升到与 Java 版本相同的服务化水平，需要补充：
1. ASP.NET Core Web API 项目作为数据平面（对标 `service-dataplane`）
2. 管理会话的 REST API + SSE 事件流端点
3. JWT 认证中间件
4. EF Core + 关系数据库持久化（替代当前仅SQLite）
5. YARP 反向代理作为 API 网关（对标 `service-gateway` 的 Spring Cloud Gateway）
6. 可选的 Blazor/Vue 管理前端
