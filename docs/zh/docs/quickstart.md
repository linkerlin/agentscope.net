---
title: "快速开始"
description: "快速上手 AgentScope .NET 2.0 —— 用 HarnessAgent 跑通第一个长期运行的智能体"
---

## 安装

AgentScope .NET 需要 .NET 8.0 SDK 及以上版本，推荐使用 dotnet CLI。

### NuGet 包

`HarnessAgent` 是推荐的入口，把工作区、长期记忆、会话持久化、子 agent、沙箱等工程能力打包在一个 builder 里；依赖 `AgentScope.Harness` 会自动把核心 `AgentScope.Core` 一并拉进来：

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Harness" Version="$(AgentScopeVersion)" />
</ItemGroup>
```

:::{note}
把 `$(AgentScopeVersion)` 替换为最新版本号即可，最新版本请参考 [Release Notes](others/release-notes.md)。
:::

如果只需要裸 `ReActAgent` 的框架 API（不需要工作区 / 持久化 / 子 agent / 沙箱），`AgentScope.Core` 足够提供 agent 本身。具体模型提供商是独立的：特定模型提供商的 Chat Model 与 formatter 位于独立的 `AgentScope.Extensions.Model.*` 模型扩展包中。`ReActAgent` 与 `HarnessAgent` 的区别详见 [Harness 架构](./harness/architecture.md)。

下面的 quickstart 通过 `.Model("dashscope:qwen-plus")` 使用 DashScope，因此还需要引入对应模型扩展：

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Model.DashScope" Version="$(AgentScopeVersion)" />
</ItemGroup>
```

MCP 集成需要官方 MCP SDK，参考 `AgentScope.Examples/AgentScope.Examples.csproj`。

## 第一个智能体

下面的例子用 `HarnessAgent` 跑通三件事：**工作区驱动的人格**（`AGENTS.md`）、**会话自动持久化**（相同 `sessionId` 的第二轮记得第一轮）、**对话压缩**（超阈值后自动压缩 + 长期事实落到 `MEMORY.md`）。模型 id 直接以字符串形式传给 `.Model(...)`，由 `ModelRegistry` 解析并自动读取对应环境变量。

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Harness.Agent;
using AgentScope.Harness.Agent.Memory.Compaction;

class FirstAgent
{
    static void Main(string[] args)
    {
        HarnessAgent agent = HarnessAgent.CreateBuilder()
                .Name("note-taker")
                .SysPrompt("你是一个帮助用户做笔记的助手。")
                // 字符串形式由 ModelRegistry 解析 —— 自动读取 DASHSCOPE_API_KEY；
                // 切换其他厂商时改用 "openai:gpt-5.5"、"anthropic:claude-sonnet-4-5"、
                // "gemini:gemini-2.0-flash" 或 "ollama:llama3"。
                .Model("dashscope:qwen-plus")
                .Workspace(Path.GetFullPath(".agentscope/workspace"))
                .Compaction(CompactionConfig.CreateBuilder()
                        .TriggerMessages(30)
                        .KeepMessages(10)
                        .Build())
                .Build();

        RuntimeContext ctx = RuntimeContext.CreateBuilder()
                .SessionId("demo-session")
                .UserId("alice")
                .Build();

        // 第一轮：自我介绍 + 当天的事
        agent.CallAsync(new UserMessage("我叫天宇，今天准备一个关于 ReAct 的技术分享。"), ctx).GetAwaiter().GetResult();

        // 第二轮：同 sessionId，自动恢复上一轮状态后回答
        agent.CallAsync(new UserMessage("我叫什么？我今天要干什么？"), ctx).GetAwaiter().GetResult();
    }
}
```

跑完之后你会看到两棵目录树——**工作区**和**状态存储**：

```
.agentscope/workspace/                          ← 工作区（agent 内容）
├── AGENTS.md                                   ← 写一份就是 agent 的人格（不写也能跑）
└── agents/note-taker/
    └── sessions/                               ← 永不压缩的原始对话日志

~/.agentscope/state/note-taker/                 ← 状态存储（在工作区外面）
└── alice/demo-session/                         ← AgentState 自动写回 / 加载
    └── agent_state.json
```

`AgentState` 默认存储在**工作区之外**的 `~/.agentscope/state/<agentId>/` 下——因为状态是恢复工作区本身的前提条件（例如沙箱清空后需要先有状态才能重建工作区），不能和工作区数据耦合。进程重启、`sessionId` 不变，第二段对话依然记得第一段。

:::{warning}
默认的 `JsonFileAgentStateStore` 是基于本地文件的实现，适用于开发和单机部署。生产集群环境请使用分布式实现，如 `RedisAgentStateStore`（由 `AgentScope.Extensions.Redis` 提供），或自行实现 `IAgentStateStore` 接口。详见[上线指南](./others/going-to-production.md)。
:::

多聊几轮触发压缩后，提炼出来的事实会先落到 `workspace/memory/YYYY-MM-DD.md`，再被周期性合并到 `MEMORY.md`，并在下一轮推理时自动注入 system prompt。

### 流式查看推理与工具调用

把 `CallAsync(...)` 换成 `StreamEventsAsync(...)` 就能实时拿到文本片段、工具调用等中间事件，适合 Web / TUI 渲染：

```csharp
using AgentScope.Core.Event;

agent.StreamEventsAsync(new UserMessage("帮我把今天的关键点列三条。"))
        .Subscribe(event =>
        {
            if (event.Type == AgentEventType.TextBlockDelta)
            {
                // 模型返回的流式文本片段 —— 追加到界面或标准输出
                Console.Write(((TextBlockDeltaEvent)event).Delta);
            }
            else if (event.Type == AgentEventType.ToolCallStart)
            {
                // 智能体即将调用工具 —— 展示调用信息
                Console.WriteLine("\n[tool] " + ((ToolCallStartEvent)event).ToolCallName);
            }
            // 其他事件：思考块、工具结果、回复结束等
        });
```

:::{tip}
运行前在环境变量里设置 `DASHSCOPE_API_KEY`。切换模型提供商时，需要引入对应的 `AgentScope.Extensions.Model.*` 模型扩展包，修改 `.Model(...)` 的字符串，并设置对应的 API key（`OPENAI_API_KEY`、`ANTHROPIC_API_KEY`、`GEMINI_API_KEY`）。需要更精细地控制超时 / 自定义 endpoint 等参数时，可使用对应模型提供商的 builder（例如 `DashScopeChatModel.CreateBuilder()...Build()`）构造实例后传给 `.Model(Model)`。
:::

### 多用户并发

Agent 在调用之间是**无状态的**——同一个实例可以处理不同用户、不同会话的请求。通过 `RuntimeContext` 传入 `userId` / `sessionId`，每次调用自动加载并隔离各自的对话上下文：

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Harness.Agent;
using AgentScope.Harness.Agent.Memory.Compaction;

// 应用启动时创建一个 agent 实例（单例即可）
HarnessAgent agent = HarnessAgent.CreateBuilder()
        .Name("note-taker")
        .SysPrompt("你是一个帮助用户做笔记的助手。")
        .Model("dashscope:qwen-plus")
        .Workspace(Path.GetFullPath(".agentscope/workspace"))
        .Compaction(CompactionConfig.CreateBuilder()
                .TriggerMessages(30)
                .KeepMessages(10)
                .Build())
        .Build();

// 在 HTTP handler 中——不同请求传入不同 RuntimeContext
agent.CallAsync(new UserMessage(userInput), RuntimeContext.CreateBuilder()
        .SessionId(sessionId)
        .UserId(userId)
        .Build()).GetAwaiter().GetResult();
```

同一 `(userId, sessionId)` 的请求自动串行化（不会并发写同一份状态）；不同 session 完全并行。完整生产部署模式（Redis session、沙箱、技能仓库等）参见[上线指南](./others/going-to-production.md)。

## 接下来

- [智能体（Agent）](./building-blocks/agent.md) —— `ReActAgent` 的完整接口、参数、`CallAsync` / `StreamEventsAsync` / `Observe`、人机交互、`IAgentStateStore` 配置
- [Harness 架构](./harness/architecture.md) —— `HarnessAgent` 的各项能力如何协作、状态如何流转
- [工作区](./harness/workspace.md) —— `AGENTS.md` / `MEMORY.md` / `skills/` / `subagents/` / `tools.json` 的目录布局与加载机制
- [文件系统](./harness/filesystem.md) —— 本机 + shell / 共享存储 / 沙箱三种部署模式
