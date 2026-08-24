---
title: "架构"
description: "HarnessAgent 组成、HarnessAgentBuilder 全量配置与中间件装配"
---

## HarnessAgent 组成

`HarnessAgent`（`AgentScope.Harness`）组合内层 `EnhancedReActAgent` 与各子系统，提供完整的智能体运行时：

```
HarnessAgent
├── EnhancedReActAgent        ← 推理-行动循环（AgentScope.Core）
├── IMessageBus               ← 消息总线（默认 WorkspaceMessageBus）
├── IFilesystem               ← 文件系统抽象（默认本地沙箱）
├── IGateway                  ← 网关（HarnessGateway，委托给内层 Agent）
└── List<IHarnessMiddleware>  ← 中间件管道（洋葱模型）
```

`HarnessAgent` 实现 `IAgent`：

- `CallAsync(IReadOnlyList<Msg>, RuntimeContext?)` / `CallAsync(Msg, ...)` / `CallAsync(string, ...)`：经过中间件管道后调用内层 Agent；
- `StreamEventsAsync(...)`：直接透传内层 Agent 的 `IAsyncEnumerable<Event>`；
- `ObserveAsync`、`Interrupt()` / `Interrupt(Msg)`。

`HarnessAgent` 构造函数是 internal，**只能通过 `HarnessAgentBuilder` 创建**。

## HarnessAgentBuilder

| 方法 | 签名 | 默认值 | 说明 |
|------|------|--------|------|
| `WithName` | `(string name)` | `"harness-agent"` | Agent 名称 |
| `WithSystemPrompt` | `(string prompt)` | 内置英文提示词 | 系统提示词 |
| `WithModel` | `(IModel model)` | **必填** | 未设置时 Build 抛异常 |
| `WithToolkit` | `(Toolkit toolkit)` | null | 一次性注入全部工具 |
| `WithPermission` | `(IPermissionEngine)` | null | 权限引擎 |
| `WithMessageBus` | `(IMessageBus bus)` | `new WorkspaceMessageBus()` | 消息总线 |
| `WithFilesystem` | `(IFilesystem fs)` | 本地当前目录文件系统 | 见[文件系统](./filesystem.md) |
| `WithDefaultFilesystem` | `(string? workspaceRoot = null)` | 当前目录 | 便捷方法：本地沙箱模式 |
| `WithTeamClient` | `(ITeamClient team)` | `new LocalTeamClient()` | 团队协作客户端 |
| `WithSubagentManager` | `(ISubagentManager mgr)` | `new DefaultAgentManager()` | 子 Agent 管理器 |
| `WithMiddleware` | `(IHarnessMiddleware mw)` | — | 追加自定义中间件，可多次 |
| `WithMaxIterations` | `(int n)` | `10` | ReAct 最大迭代 |
| `WithWorkspace` | `(WorkspaceManager mgr)` | null | 启用工作区三中间件 |
| `WithWorkspaceRoot` | `(string root, bool sandboxed = true)` | — | 便捷重载：`new WorkspaceManager(root, sandboxed)` |
| `WithToolResultEviction` | `(ToolResultEvictionConfig cfg)` | null | 启用大工具结果驱逐 |
| `WithMemoryConsolidator` | `(MemoryConsolidator c)` | null | 记忆整合器（供维护中间件调用） |
| `WithSkillUsageStore` | `(SkillUsageStore store)` | null | 启用技能使用统计中间件 |
| `WithSkillCurator` | `(SkillCurator curator)` | null | 启用技能策展中间件 |
| `Build` | `()` | — | 组装并返回 `HarnessAgent` |

### 典型装配

```csharp
using AgentScope.Harness;
using AgentScope.Harness.Middleware;

HarnessAgent agent = new HarnessAgentBuilder()
    .WithName("coder")
    .WithSystemPrompt("你是一个编码助手。")
    .WithModel(model)
    .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
    .WithMaxIterations(20)
    .WithMiddleware(new CompactionMiddleware(maxContextLength: 8192))
    .Build();
```

## Build() 装配细节

`Build()` 做四件事：

1. **构建内层 `EnhancedReActAgent`**：应用 `Name` / `SysPrompt` / `Model` / 工具 / 权限 / `MaxIterations`；
2. **创建 `HarnessGateway`** 包装内层 Agent；
3. **装配中间件管道**：先加入用户通过 `WithMiddleware` 传入的中间件，再自动追加 `SandboxLifecycle` → `Subagents` → `Teams` → `Inbox` → `PlanMode` → `Compaction` → `MemoryFlush` → `AgentTrace` → `Transcript`；配置了工作区时再追加 `WorkspaceContext` / `AtPathExpansion` / `MemoryMaintenance`；显式配置了驱逐 / 技能统计 / 技能策展时追加对应中间件；
4. **构造 `HarnessAgent`**。

执行时中间件按 `Order` 升序组成洋葱链（见 [Middleware](../building-blocks/middleware.md)），系统提示词先经每层 `OnSystemPromptAsync` 依次改写后写回内层 Agent。

## 网关（Gateway）

`IGateway` 把 Agent 暴露为统一入口：

```csharp
public interface IGateway
{
    Task<Msg> RunAsync(Msg input, RuntimeContext? context = null, CancellationToken ct = default);
    IAsyncEnumerable<Event> RunStreamAsync(Msg input, RuntimeContext? context = null, CancellationToken ct = default);
}
```

`HarnessGateway(IAgent agent)` 是默认实现，直接委托给内层 Agent 的 `CallAsync` / `StreamEventsAsync`。Channel（见[Channel](./channel.md)）通过网关与 Agent 交互。

## 消息总线（IMessageBus）

`WorkspaceMessageBus`（默认）基于 `System.Threading.Channels`，支持四种模式：

- **队列（Drain queue）**：`QueuePushAsync(queue, entry)` / `QueueDrainAsync(queue)` / `QueueDeleteAsync`；
- **回放日志（Replay log）**：`LogAppendAsync(log, entry)` / `LogReadAsync(log, startSeq)` / `LogTrimAsync`；
- **发布订阅**：`PublishAsync(topic, entry)` / `Subscribe(topic, handler)`；
- **收件箱领域助手**：`InboxPushAsync(agentId, entry)` / `InboxDrainAsync(agentId)`（`InboxMiddleware` 在每回合开始时消费）。

条目类型 `BusEntry(Id, Key, Payload)`，带单调 `Sequence`。

## 相关文档

- [Middleware](../building-blocks/middleware.md) —— 中间件接口与 Order 表
- [文件系统](./filesystem.md) · [工作区](./workspace.md) · [子 Agent](./subagent.md) · [Channel](./channel.md)
