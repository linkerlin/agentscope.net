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

## 技术栈

C# (.NET 9.0/10.0) | EF Core | SQLite | System.Reactive | Terminal.Gui | Uno Platform | xUnit | System.Text.Json

## 许可证

Apache License 2.0
