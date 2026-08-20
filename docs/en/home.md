# AgentScope .NET

The enterprise-grade agent framework for .NET, a .NET counterpart implementation of [agentscope-java](https://github.com/agentscope-ai/agentscope-java).

## Core Features

- **EnhancedReActAgent** — Reasoning-Action (ReAct) loop engine with streaming events, Hooks, permissions, and structured output
- **HarnessAgent** — Complete runtime assembling workspace, filesystem, message bus, sub-agents, teams, and middleware pipeline
- **Message System** — `Msg` / `ContentBlock` unified multimodal messages (text, image, audio, video, tool calls and results)
- **Model Layer** — OpenAI / DashScope / Anthropic / Gemini / DeepSeek / Ollama / Mock, unified `IModel` + `IStreamingChatModel`
- **Tool System** — `[Tool]` attribute auto-registration, `Toolkit` group management, built-in file / Shell / search tools
- **MCP Protocol** — `McpClientBuilder` supports Stdio / SSE / Streamable HTTP three transports
- **Permission System** — Allow / Ask / Deny three-state decisions with human-in-the-loop (HITL) confirmation
- **Skill System** — Markdown skill repository (`.agentscope/skills`) with on-demand loading
- **Sub-agents / Teams** — `SubagentDeclaration` declarative sub-agents, `LocalTeamClient` in-process collaboration
- **Persistence** — `IAgentStateStore` (InMemory / JSON file / Redis / MySQL / PostgreSQL / OSS / COS)
- **Observability** — Jsonl Tracing built-in, OpenTelemetry extension optional

## Supported Models

| Provider | Model Class | Default BaseUrl |
|----------|-------------|-----------------|
| OpenAI | `OpenAIModel` | Official API |
| DashScope (Tongyi Qianwen) | `DashScopeModel` | `https://dashscope.aliyuncs.com` |
| Anthropic (Claude) | `AnthropicModel` | `https://api.anthropic.com` |
| Gemini | `GeminiModel` | — |
| DeepSeek | `DeepSeekModel` (inherits OpenAIModel) | DeepSeek API |
| Ollama (local) | `OllamaModel` (inherits OpenAIModel) | Local port 11434 |
| Mock (testing) | `MockModel` | — |

All models are inside the `AgentScope.Core` package; no additional model extension packages are needed.

## Quick Start

```bash
dotnet build src/AgentScope.Core/AgentScope.Core.csproj
dotnet run --project examples/QuickStart/QuickStart.csproj
```

Minimal runnable example (using the same building APIs as `examples/AgentScope.Lab`):

```csharp
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Harness;
using AgentScope.Harness.Middleware;

HarnessAgent agent = new HarnessAgentBuilder()
    .WithName("note-taker")
    .WithSystemPrompt("You are an assistant that helps users take notes.")
    .WithModel(new DashScopeModel("qwen-plus", apiKey))   // Use MockModel.Builder().Build() when no key is available
    .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
    .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))
    .Build();

RuntimeContext ctx = RuntimeContext.Empty
    .WithUserId("alice")
    .WithSessionId("demo-session");

Msg reply = await agent.CallAsync(
    Msg.Builder().Role("user").TextContent("Hello!").Build(), ctx);
Console.WriteLine(reply.GetTextContent());
```

## Documentation Navigation

- [Quickstart](docs/quickstart.md)
- [What is AgentScope 2.0?](docs/index.md)
- **Core Components**
  - [Agent](docs/building-blocks/agent.md)
  - [Message and Event](docs/building-blocks/message-and-event.md)
  - [Middleware](docs/building-blocks/middleware.md)
  - [Model](docs/building-blocks/model.md)
  - [Permission System](docs/building-blocks/permission-system.md)
  - [Tool](docs/building-blocks/tool.md)
  - [Context and AgentState](docs/building-blocks/context.md)
- **Harness**
  - [Architecture](docs/harness/architecture.md) · [Workspace](docs/harness/workspace.md) · [Memory](docs/harness/memory.md)
  - [Filesystem](docs/harness/filesystem.md) · [Sandbox](docs/harness/sandbox.md) · [Sub-agent](docs/harness/subagent.md)
  - [Skill](docs/harness/skill.md) · [Plan Mode](docs/harness/plan-mode.md) · [Channel](docs/harness/channel.md) · [Context Compaction](docs/harness/compaction.md)
