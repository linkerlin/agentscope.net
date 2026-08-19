# AgentScope .NET

基于 .NET 的企业级、分布式智能体框架，[agentscope-java](https://github.com/agentscope-ai/agentscope-java) 的 .NET 移植版。

## 核心特性

- **ReActAgent / EnhancedReActAgent** — 推理-行动循环引擎
- **消息系统** — Msg / ContentBlock 统一多模态消息
- **Pipeline / Workflow** — 7 种管道节点 + DAG 工作流
- **RAG** — 向量存储 + 知识检索
- **Skill 系统** — 技能仓库与动态加载
- **MCP 协议** — stdio / SSE / HTTP 三种传输
- **A2A 协议** — 智能体间通信
- **权限系统** — 允许 / 审批 / 拒绝三态决策
- **多智能体** — AgentGroup / MsgHub / 路由
- **可观测性** — OpenTelemetry Tracing

## 支持的模型

| 提供商 | 说明 |
|--------|------|
| DeepSeek | deepseek-chat / reasoner |
| OpenAI | GPT-3.5 / 4 |
| Azure OpenAI | |
| Anthropic | Claude 系列 |
| DashScope | 通义千问 |
| Gemini | Pro / Flash |
| Ollama | 本地 LLM |

## 快速开始

```bash
dotnet build src/AgentScope.Core/AgentScope.Core.csproj
dotnet run --project examples/QuickStart/QuickStart.csproj
```

```csharp
var agent = ReActAgent.CreateBuilder()
    .Name("assistant")
    .Model("dashscope:qwen-max")
    .Build();

var reply = await agent.CallAsync(Msg.GetUserMessage("你好！"));
```

## 文档导航

- [快速上手](zh/docs/quickstart.md)
- [什么是 AgentScope 2.0？](zh/docs/index.md)
- **核心组件**
  - [智能体](zh/docs/building-blocks/agent.md)
  - [消息与事件](zh/docs/building-blocks/message-and-event.md)
  - [Middleware](zh/docs/building-blocks/middleware.md)
  - [模型](zh/docs/building-blocks/model.md)
  - [权限系统](zh/docs/building-blocks/permission-system.md)
  - [工具](zh/docs/building-blocks/tool.md)
  - [上下文与 AgentState](zh/docs/building-blocks/context.md)
- **Harness**
  - [架构](zh/docs/harness/architecture.md) · [工作区](zh/docs/harness/workspace.md) · [记忆](zh/docs/harness/memory.md)
  - [沙箱](zh/docs/harness/sandbox.md) · [子 Agent](zh/docs/harness/subagent.md) · [技能](zh/docs/harness/skill.md)
  - [计划模式](zh/docs/harness/plan-mode.md) · [Channel](zh/docs/harness/channel.md) · [上下文压缩](zh/docs/harness/compaction.md)
