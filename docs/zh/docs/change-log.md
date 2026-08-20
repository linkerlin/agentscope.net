---
title: "变更说明"
description: "AgentScope .NET 2.0 相对 1.x 的 API 变更与迁移指引"
---

:::{tip}
各版本的逐条变更记录见 [Release Notes](others/release-notes.md)。本页聚焦从 1.x 迁到 2.0（当前代码版本 2.0.1，目标框架 `net10.0`）时需要注意的破坏性变化。
:::

## 必须迁移（编译失败或行为改变）

### A.1 使用 `EnhancedReActAgent` 取代 `ReActAgent`

`ReActAgent` 已整体标记 `[Obsolete]`，请改用 `EnhancedReActAgent`（`AgentScope.Core`）：

```csharp
// 1.x
ReActAgent agent = ReActAgent.Builder().Name("a").Model(model).Build();

// 2.0
EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Name("a")
    .Model(model)
    .Build();
```

Builder 方法名不变（`Name` / `Model` / `SysPrompt` / `Memory` / `AddTool` / `MaxIterations`），新增 `HookManager(...)` / `PermissionEngine(...)` / `StatePersistence(...)` / `Verbose(...)` / `ConfirmCallback(...)` / `AutoApproveOnAsk(...)` / `ToolGroupManager(...)` / `AddToolGroup(...)`。

### A.2 `RuntimeContext` 改为不可变 record

1.x 的 `RuntimeContext.Builder().WithUserId(...).WithSessionId(...).Build()` 不再存在；2.0 用 record 派生：

```csharp
RuntimeContext ctx = RuntimeContext.Empty.WithUserId("alice").WithSessionId("s1");
```

读取会话字段用属性 `ctx.UserId` / `ctx.SessionId`；静态 `RuntimeContext.Current`（AsyncLocal）在异步链路中流转。**没有** `Put` / `Get<T>` 属性袋。

### A.3 模型不再是字符串 id

`ModelRegistry` 字符串解析（`.Model("dashscope:qwen-plus")`）已移除。模型直接构造实例：

```csharp
IModel model = new DashScopeModel("qwen-plus", apiKey);
// 或 ModelFactory.Create("dashscope", "qwen-plus", apiKey)
```

所有模型（OpenAI / DashScope / Anthropic / Gemini / DeepSeek / Ollama / Mock）都在 `AgentScope.Core` 内；**不存在** `AgentScope.Extensions.Model.*` 包。

### A.4 流式事件模型改变

1.x 的 `StreamAsync(...)`（产出细粒度 AgentEvent）已废弃，改用：

```csharp
await foreach (Event evt in agent.StreamEventsAsync(msg)) { }
```

`Event`（`AgentScope.Core.Events`）带 `Type`（`EventType` 枚举：Reasoning/ToolCall/Acting/Summary/Error × Start/Chunk/Finish）、`Message`、`IsLast`、`Metadata`。细粒度 `AgentEvent` record 层次保留，但 ReAct 循环不再产出它们（用于协议适配层）。

### A.5 `HarnessAgent` 构建方式

1.x 的 `HarnessAgent.CreateBuilder()...` 不存在；2.0：

```csharp
HarnessAgent agent = new HarnessAgentBuilder()
    .WithName("note-taker")
    .WithSystemPrompt("...")
    .WithModel(model)
    .WithWorkspaceRoot(".agentscope/workspace")
    .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))
    .Build();
```

全部方法均以 `With` 开头：`WithName` / `WithSystemPrompt` / `WithModel` / `WithToolkit` / `WithPermission` / `WithMessageBus` / `WithFilesystem` / `WithDefaultFilesystem` / `WithTeamClient` / `WithSubagentManager` / `WithMiddleware` / `WithMaxIterations` / `WithWorkspace` / `WithWorkspaceRoot` / `WithToolResultEviction` / `WithMemoryConsolidator` / `WithSkillUsageStore` / `WithSkillCurator` / `Build`。

### A.6 消息构造

`new UserMessage("纯文本")` 单参构造不存在（`UserMessage` 只有无参或 `(name, content)`）。统一使用：

```csharp
Msg msg = Msg.Builder().Role("user").TextContent("...").Build();
```

### A.7 状态持久化方式

`AgentStateStore` 自动挂载（builder 上的 `StateStore(...)`）已移除。2.0 的状态能力：

- **记忆持久化**：Builder `Memory(IMemory)` 注入 `SqliteMemory(path)`、`StateBackedMemory(store, initial, key)`（自动写 `IAgentStateStore`）等；
- **会话保存**：`EnhancedReActAgent.SaveTo / LoadFrom / LoadIfExists(Session, sessionKey)` + `SessionManager`；
- **分布式存储**：`AgentScope.Extensions.Store.*` 的 `XxxAgentStateStore` 实现 `IAgentStateStore`。

### A.8 包命名

1.x 的 `AgentScope.Extensions.Redis` / `AgentScope.Extensions.MySql` 等改为按用途分仓：

| 能力 | 2.0 包 |
|------|--------|
| 分布式状态存储 | `AgentScope.Extensions.Store.Redis` / `Store.MySql` / `Store.PostgreSql` / `Store.Oss` / `Store.Cos` |
| 向量检索 | `AgentScope.Extensions.Vector.Elasticsearch` / `Vector.Milvus` / `Vector.PgVector` / `Vector.Qdrant` |
| 技能仓库 | `AgentScope.Extensions.Skill.Git` / `Skill.MySql` / `Skill.PostgreSql` |
| 即时通讯渠道 | `AgentScope.Extensions.Channel.DingTalk` / `Channel.Feishu` / `Channel.WeCom` / `Channel.GitHub` / `Channel.GitLab` |
| 沙箱 | `AgentScope.Extensions.Sandbox.Docker` / `Sandbox.E2B` / `Sandbox.Daytona` / `Sandbox.AgentRun` / `Sandbox.Kubernetes` |
| 调度 | `AgentScope.Extensions.Scheduler.Quartz` / `Scheduler.XxlJob` |
| 可观测性 | `AgentScope.Tracing.OpenTelemetry` |

## 推荐迁移

- `StreamAsync` / `StreamStructuredOutputAsync`（细粒度事件）→ `StreamEventsAsync`；
- 直接订阅模型事件 → 优先使用 `StreamEventsAsync` 的粗粒度事件；
- `AgentEvent` 细粒度 record 仅在协议适配层使用。

## 新增能力

- **中间件管道**：`IHarnessMiddleware` 四挂点（OnAgent / OnModelCall / OnToolExecution / OnSystemPrompt），Order 排序，洋葱模型；
- **MCP 客户端**：`McpClientBuilder.Create()` 支持 Stdio / Streamable HTTP / SSE；
- **结构化输出**：`GenerateStructuredOutputAsync<T>`；
- **Hook 体系**：`HookManager` + 11 个生命周期回调；
- **技能策展**：`SkillCurator` / `SkillUsageStore`；
- **A2A / AgUI / Agent Protocol**：`AgentScope.Core` 内置协议适配（A2A 客户端与服务器、AgUI 适配器）；
- **追踪**：`AgentScope.Core.Tracing`（Jsonl 导出器）+ OpenTelemetry 中间件；
- **计划模式**：`PlanModeManager` + `plan_mode_toggle` / `plan_mode_query` 工具；
- **团队协作**：`ITeamClient` / `LocalTeamClient`。
