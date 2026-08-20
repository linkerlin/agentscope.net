# AgentScope .NET

基于 .NET 的企业级智能体框架，[agentscope-java](https://github.com/agentscope-ai/agentscope-java) 的 .NET 对标实现。

## 核心特性

- **EnhancedReActAgent** — 推理-行动（ReAct）循环引擎，支持流式事件、Hook、权限、结构化输出
- **HarnessAgent** — 组装工作区、文件系统、消息总线、子 Agent、团队与中间件管道的完整运行时
- **消息系统** — `Msg` / `ContentBlock` 统一多模态消息（文本、图片、音视频、工具调用与结果）
- **模型层** — OpenAI / DashScope / Anthropic / Gemini / DeepSeek / Ollama / Mock，统一 `IModel` + `IStreamingChatModel`
- **工具系统** — `[Tool]` 特性自动注册、`Toolkit` 分组管理、内置文件 / Shell / 搜索工具
- **MCP 协议** — `McpClientBuilder` 支持 Stdio / SSE / Streamable HTTP 三种传输
- **权限系统** — 允许 / 询问 / 拒绝三态决策，支持人机交互确认（HITL）
- **技能系统** — Markdown 技能仓库（`.agentscope/skills`）与按需加载
- **子 Agent / 团队** — `SubagentDeclaration` 声明式子 Agent，`LocalTeamClient` 进程内协作
- **持久化** — `IAgentStateStore`（内存 / JSON 文件 / Redis / MySQL / PostgreSQL / OSS / COS）
- **可观测性** — Jsonl Tracing 内置，OpenTelemetry 扩展可选

## 支持的模型

| 提供商 | 模型类 | 默认 BaseUrl |
|--------|--------|--------------|
| OpenAI | `OpenAIModel` | 官方 API |
| DashScope（通义千问） | `DashScopeModel` | `https://dashscope.aliyuncs.com` |
| Anthropic（Claude） | `AnthropicModel` | `https://api.anthropic.com` |
| Gemini | `GeminiModel` | — |
| DeepSeek | `DeepSeekModel`（继承 OpenAIModel） | DeepSeek API |
| Ollama（本地） | `OllamaModel`（继承 OpenAIModel） | 本机 11434 端口 |
| Mock（测试） | `MockModel` | — |

所有模型都在 `AgentScope.Core` 包内，无需额外安装模型扩展包。

## 快速开始

```bash
dotnet build src/AgentScope.Core/AgentScope.Core.csproj
dotnet run --project examples/QuickStart/QuickStart.csproj
```

最小可运行示例（与 `examples/AgentScope.Lab` 使用相同的构建 API）：

```csharp
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Harness;
using AgentScope.Harness.Middleware;

HarnessAgent agent = new HarnessAgentBuilder()
    .WithName("note-taker")
    .WithSystemPrompt("你是一个帮助用户做笔记的助手。")
    .WithModel(new DashScopeModel("qwen-plus", apiKey))   // 缺 Key 时可用 MockModel.Builder().Build()
    .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
    .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))
    .Build();

RuntimeContext ctx = RuntimeContext.Empty
    .WithUserId("alice")
    .WithSessionId("demo-session");

Msg reply = await agent.CallAsync(
    Msg.Builder().Role("user").TextContent("你好！").Build(), ctx);
Console.WriteLine(reply.GetTextContent());
```

## 文档导航

- [快速上手](docs/quickstart.md)
- [什么是 AgentScope 2.0？](docs/index.md)
- **核心组件**
  - [智能体](docs/building-blocks/agent.md)
  - [消息与事件](docs/building-blocks/message-and-event.md)
  - [Middleware](docs/building-blocks/middleware.md)
  - [模型](docs/building-blocks/model.md)
  - [权限系统](docs/building-blocks/permission-system.md)
  - [工具](docs/building-blocks/tool.md)
  - [上下文与 AgentState](docs/building-blocks/context.md)
- **Harness**
  - [架构](docs/harness/architecture.md) · [工作区](docs/harness/workspace.md) · [记忆](docs/harness/memory.md)
  - [文件系统](docs/harness/filesystem.md) · [沙箱](docs/harness/sandbox.md) · [子 Agent](docs/harness/subagent.md)
  - [技能](docs/harness/skill.md) · [计划模式](docs/harness/plan-mode.md) · [Channel](docs/harness/channel.md) · [上下文压缩](docs/harness/compaction.md)
