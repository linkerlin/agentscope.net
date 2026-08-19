# AgentScope .NET

An enterprise-grade, distributed agent framework for .NET — a port of [agentscope-java](https://github.com/agentscope-ai/agentscope-java).

## Core Features

- **ReActAgent / EnhancedReActAgent** — reasoning-acting loop engine
- **Message System** — unified multimodal Msg / ContentBlock
- **Pipeline / Workflow** — 7 pipeline nodes + DAG workflow
- **RAG** — vector store + knowledge retrieval
- **Skill System** — skill repository with dynamic loading
- **MCP Protocol** — stdio / SSE / HTTP transports
- **A2A Protocol** — agent-to-agent communication
- **Permission System** — allow / approve / deny decisions
- **Multi-Agent** — AgentGroup / MsgHub / routing
- **Observability** — OpenTelemetry tracing

## Supported Models

| Provider | Notes |
|----------|-------|
| DeepSeek | deepseek-chat / reasoner |
| OpenAI | GPT-3.5 / 4 |
| Azure OpenAI | |
| Anthropic | Claude series |
| DashScope | Qwen |
| Gemini | Pro / Flash |
| Ollama | local LLMs |

## Quick Start

```bash
dotnet build src/AgentScope.Core/AgentScope.Core.csproj
dotnet run --project examples/QuickStart/QuickStart.csproj
```

```csharp
var agent = ReActAgent.CreateBuilder()
    .Name("assistant")
    .Model("dashscope:qwen-max")
    .Build();

var reply = await agent.CallAsync(Msg.GetUserMessage("Hello!"));
```

## Documentation

- [Quickstart](en/docs/quickstart.md)
- [What's AgentScope 2.0?](en/docs/index.md)
- **Building Blocks**
  - [Agent](en/docs/building-blocks/agent.md)
  - [Message & Event](en/docs/building-blocks/message-and-event.md)
  - [Middleware](en/docs/building-blocks/middleware.md)
  - [Model](en/docs/building-blocks/model.md)
  - [Permission System](en/docs/building-blocks/permission-system.md)
  - [Tool](en/docs/building-blocks/tool.md)
  - [Context & AgentState](en/docs/building-blocks/context.md)
- **Harness**
  - [Architecture](en/docs/harness/architecture.md) · [Workspace](en/docs/harness/workspace.md) · [Memory](en/docs/harness/memory.md)
  - [Sandbox](en/docs/harness/sandbox.md) · [Subagent](en/docs/harness/subagent.md) · [Skill](en/docs/harness/skill.md)
  - [Plan Mode](en/docs/harness/plan-mode.md) · [Channel](en/docs/harness/channel.md) · [Compaction](en/docs/harness/compaction.md)
