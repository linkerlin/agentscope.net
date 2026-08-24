# AgentScope.NET

基于 .NET 的 AgentScope 框架，[agentscope-java](https://github.com/agentscope-ai/agentscope-java) 1:1 移植。

A .NET implementation of the AgentScope framework for building LLM-powered applications.

## 项目状态

**版本**: v2.0.1 | **分支**: develop/v2.0.1 | **核心模块**: 22/22 全部完成
**代码**: 959 .cs / ~65,941 行 | **扩展**: 42 个 | **Core 构建**: ✅ 0 错误
**完整方案**: 🔴 118 错误 / 230 警告 (仅测试 Mock + Uno XAML)

## 支持的 LLM 提供商

| 提供商 | 状态 |
|--------|------|
| DeepSeek | ✅ deepseek-chat/reasoner |
| OpenAI | ✅ GPT-3.5/4 |
| Azure OpenAI | ✅ |
| Anthropic | ✅ Claude 系列 |
| DashScope | ✅ 通义千问 |
| Gemini | ✅ Pro/Flash |
| Ollama | ✅ 本地 LLM |

## 特性

- EnhancedReActAgent / Hook / Session / State / SQLite Memory
- 消息系统 (Msg/ContentBlock) / Pipeline (7节点) / Plan (PlanNotebook)
- RAG (向量存储+检索) / Workflow (DAG) / MultiAgent
- Skill 系统 / MCP 协议 (stdio/SSE/HTTP) / A2A 协议
- TUI (Terminal.Gui) / Uno Platform GUI (XAML待修)
- OpenTelemetry Tracing / Harness 运行时框架

## 快速开始

```bash
dotnet build src/AgentScope.Core/AgentScope.Core.csproj
dotnet run --project examples/QuickStart/QuickStart.csproj
dotnet run --project examples/WebSocketEcho/WebSocketEcho.csproj
```

## 文档

文档位于 `docs/` 目录，中英文各 82 篇：

```
docs/
├── index.html          # 本地文档站入口（浏览器渲染）
├── en/                 # 英文文档（82 篇）
│   ├── home.md         # 英文首页
│   ├── docs/           # quickstart / building-blocks / harness / reference
│   └── integration/    # model / channel / rag / memory / session / skill / protocol 等
├── zh/                 # 中文文档（82 篇，结构与 en/ 一致）
│   ├── home.md         # 中文首页
│   └── ...
└── capability-status.md
```

### 方法一：浏览器查看（推荐）

启动本地文档服务（需 Node.js）：

```bash
npx -y http-server docs -p 3001 -c-1
```

打开 http://localhost:3001 即可看到带侧边栏导航、Markdown 渲染和代码高亮的文档站。按 `Ctrl+C` 停止服务。

### 方法二：VS Code 查看

1. VS Code 打开 `docs/` 文件夹
2. 点击任意 `.md` 文件
3. 按 `Ctrl+Shift+V` 预览渲染效果（或点右上角"打开预览"按钮）

## 与 Java 版 (agentscope-java) 差异

### 模块对标

| Java 模块 | C# 对应工程 | 对标程度 |
|-----------|-----------|---------|
| `agentscope-core` | `AgentScope.Core` | ✅ 完全对标 |
| `agentscope-harness` | `AgentScope.Harness` | ✅ 完全对标 |
| `agentscope-extensions` | `AgentScope.Extensions` + 扩展子项目 | ✅ 完全对标 |
| `agentscope-service` | **无直接对标**¹ | ❌ |
| `agentscope-spring-boot-starters` | 无 | ❌ |

> ¹ Java `agentscope-service` 是 Spring Boot 4 微服务部署平台（四平面架构：gateway/dataplane/scheduler/common），功能分散在 C# 的 `AgentScope.Core.Service` + `AgentScope.Harness` + `AgentScope.Extensions.Channel.*` 中，但 C# **不包含** API 网关、JPA 持久化、JWT 认证、管理 REST API、SSE 端点和 Cron 调度等生产级部署组件。

### C# 独有功能

| 功能 | 位置 | 说明 |
|------|------|------|
| A2A 完整协议栈 | `AgentScope.Core/A2A/` | 同时包含 Server + Client |
| Pipeline 管道系统 | `AgentScope.Core/Pipeline/` | 节点编排引擎 |
| Workflow 工作流 | `AgentScope.Core/Workflow/` | DAG 工作流引擎 |
| TUI 终端应用 | `AgentScope.TUI/` | Terminal.Gui 交互界面 |
| Uno 桌面应用 | `AgentScope.Uno/` | 跨平台桌面 GUI |
| Docker 沙箱 | `AgentScope.Extensions.Sandbox.Docker/` | Java 无此沙箱 |
| DeepSeek 模型 | `AgentScope.Core.Model.DeepSeek/` | Java 版未内置 |
| XXL-JOB 调度 | `AgentScope.Extensions.Scheduler.XxlJob/` | Java 版仅有 Quartz |

### Java 独有功能

| 功能 | 位置 | 说明 |
|------|------|------|
| agentscope-service 微服务平台 | `agentscope-service/` | Spring Boot 4 部署平台 |
| Spring Boot Starters | `agentscope-spring-boot-starters/` | 11 个自动配置 Starter |
| Hands Workers | `service-scheduler/worker/` | 独立 Worker 进程 |
| Cron 部署调度 | `service-scheduler/` | 周期性 Agent 部署 |
| JPA 持久化 | `service-common/persistence/` | 43 个 JPA 实体 |
| JWT 认证/ACL | `service-common/auth/` | 完整认证授权 |
| SSE 事件流 | `service-dataplane/` | 实时流推送 |
| 管理 REST API | `service-dataplane/` | 完整会话管理 API |

### 架构差异本质

```
Java: Core → Harness → agentscope-service (Spring Boot 微服务平台)
C#:   Core → Harness → [需用户自行编写 Web 宿主]
```

Java 将 Harness 引擎包装在 Spring Boot 微服务中形成可部署生产平台；C# 仅提供类库，没有内建微服务包装层，需用户自行编写 ASP.NET Core 宿主程序。

## 技术栈

C# (.NET 9.0/10.0) | EF Core | SQLite | System.Reactive | Terminal.Gui | Uno Platform | xUnit | System.Text.Json

## 许可证

Apache License 2.0
