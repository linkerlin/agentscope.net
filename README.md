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

## 技术栈

C# (.NET 9.0/10.0) | EF Core | SQLite | System.Reactive | Terminal.Gui | Uno Platform | xUnit | System.Text.Json

## 许可证

Apache License 2.0
