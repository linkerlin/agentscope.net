---
title: "Change Log"
description: "AgentScope .NET 2.0 API changes and migration guide from 1.x"
---

:::{tip}
See [Release Notes](others/release-notes.md) for per-version changelogs. This page focuses on breaking changes when migrating from 1.x to 2.0 (current code version 2.0.1, target framework `net10.0`).
:::

## Must Migrate (Compilation failure or behavior change)

### A.1 Use `EnhancedReActAgent` instead of `ReActAgent`

`ReActAgent` is now marked `[Obsolete]`. Use `EnhancedReActAgent` (`AgentScope.Core`):

```csharp
// 1.x
ReActAgent agent = ReActAgent.Builder().Name("a").Model(model).Build();

// 2.0
EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Name("a")
    .Model(model)
    .Build();
```

Builder method names remain the same (`Name` / `Model` / `SysPrompt` / `Memory` / `AddTool` / `MaxIterations`), with new additions: `HookManager(...)` / `PermissionEngine(...)` / `StatePersistence(...)` / `Verbose(...)` / `ConfirmCallback(...)` / `AutoApproveOnAsk(...)` / `ToolGroupManager(...)` / `AddToolGroup(...)`.

### A.2 `RuntimeContext` Changed to Immutable Record

1.x's `RuntimeContext.Builder().WithUserId(...).WithSessionId(...).Build()` no longer exists; 2.0 uses record derivation:

```csharp
RuntimeContext ctx = RuntimeContext.Empty.WithUserId("alice").WithSessionId("s1");
```

Read session fields via properties `ctx.UserId` / `ctx.SessionId`; static `RuntimeContext.Current` (AsyncLocal) flows across async contexts. **No** `Put` / `Get<T>` property bag.

### A.3 Model Is No Longer a String ID

`ModelRegistry` string parsing (`.Model("dashscope:qwen-plus")`) has been removed. Models are constructed as instances directly:

```csharp
IModel model = new DashScopeModel("qwen-plus", apiKey);
// or ModelFactory.Create("dashscope", "qwen-plus", apiKey)
```

All models (OpenAI / DashScope / Anthropic / Gemini / DeepSeek / Ollama / Mock) are inside `AgentScope.Core`; **no** `AgentScope.Extensions.Model.*` package exists.

### A.4 Streaming Event Model Changed

1.x's `StreamAsync(...)` (producing fine-grained AgentEvent) is deprecated. Use:

```csharp
await foreach (Event evt in agent.StreamEventsAsync(msg)) { }
```

`Event` (`AgentScope.Core.Events`) carries `Type` (`EventType` enum: Reasoning/ToolCall/Acting/Summary/Error × Start/Chunk/Finish), `Message`, `IsLast`, `Metadata`. The fine-grained `AgentEvent` record hierarchy is retained but no longer produced by the ReAct loop (used for protocol adaptation layer).

### A.5 `HarnessAgent` Construction

1.x's `HarnessAgent.CreateBuilder()...` no longer exists; 2.0:

```csharp
HarnessAgent agent = new HarnessAgentBuilder()
    .WithName("note-taker")
    .WithSystemPrompt("...")
    .WithModel(model)
    .WithWorkspaceRoot(".agentscope/workspace")
    .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))
    .Build();
```

All methods start with `With`: `WithName` / `WithSystemPrompt` / `WithModel` / `WithToolkit` / `WithPermission` / `WithMessageBus` / `WithFilesystem` / `WithDefaultFilesystem` / `WithTeamClient` / `WithSubagentManager` / `WithMiddleware` / `WithMaxIterations` / `WithWorkspace` / `WithWorkspaceRoot` / `WithToolResultEviction` / `WithMemoryConsolidator` / `WithSkillUsageStore` / `WithSkillCurator` / `Build`.

### A.6 Message Construction

`new UserMessage("text")` single-arg constructor no longer exists (`UserMessage` only has parameterless or `(name, content)`). Use:

```csharp
Msg msg = Msg.Builder().Role("user").TextContent("...").Build();
```

### A.7 State Persistence

`AgentStateStore` auto-mounting (builder's `StateStore(...)`) has been removed. 2.0 state capabilities:

- **Memory persistence**: Builder `Memory(IMemory)` injects `SqliteMemory(path)`, `StateBackedMemory(store, initial, key)` (auto-writes to `IAgentStateStore`), etc.;
- **Session saving**: `EnhancedReActAgent.SaveTo / LoadFrom / LoadIfExists(Session, sessionKey)` + `SessionManager`;
- **Distributed storage**: `AgentScope.Extensions.Store.*`'s `XxxAgentStateStore` implements `IAgentStateStore`.

### A.8 Package Naming

1.x's `AgentScope.Extensions.Redis` / `AgentScope.Extensions.MySql` etc. are reorganized by purpose:

| Capability | 2.0 Package |
|------|--------|
| Distributed state store | `AgentScope.Extensions.Store.Redis` / `Store.MySql` / `Store.PostgreSql` / `Store.Oss` / `Store.Cos` |
| Vector search | `AgentScope.Extensions.Vector.Elasticsearch` / `Vector.Milvus` / `Vector.PgVector` / `Vector.Qdrant` |
| Skill repository | `AgentScope.Extensions.Skill.Git` / `Skill.MySql` / `Skill.PostgreSql` |
| IM channels | `AgentScope.Extensions.Channel.DingTalk` / `Channel.Feishu` / `Channel.WeCom` / `Channel.GitHub` / `Channel.GitLab` |
| Sandbox | `AgentScope.Extensions.Sandbox.Docker` / `Sandbox.E2B` / `Sandbox.Daytona` / `Sandbox.AgentRun` / `Sandbox.Kubernetes` |
| Scheduler | `AgentScope.Extensions.Scheduler.Quartz` / `Scheduler.XxlJob` |
| Observability | `AgentScope.Tracing.OpenTelemetry` |

## Recommended Migration

- `StreamAsync` / `StreamStructuredOutputAsync` (fine-grained events) → `StreamEventsAsync`;
- Direct model event subscription → prefer `StreamEventsAsync` coarse-grained events;
- `AgentEvent` fine-grained records are used only in the protocol adaptation layer.

## New Capabilities

- **Middleware pipeline**: `IHarnessMiddleware` four hooks (OnAgent / OnModelCall / OnToolExecution / OnSystemPrompt), Order-sorted, onion model;
- **MCP client**: `McpClientBuilder.Create()` supports Stdio / Streamable HTTP / SSE;
- **Structured output**: `GenerateStructuredOutputAsync<T>`;
- **Hook system**: `HookManager` + 11 lifecycle callbacks;
- **Skill curation**: `SkillCurator` / `SkillUsageStore`;
- **A2A / AgUI / Agent Protocol**: `AgentScope.Core` built-in protocol adaptation (A2A client and server, AgUI adapter);
- **Tracing**: `AgentScope.Core.Tracing` (Jsonl exporter) + OpenTelemetry middleware;
- **Plan mode**: `PlanModeManager` + `plan_mode_toggle` / `plan_mode_query` tools;
- **Team collaboration**: `ITeamClient` / `LocalTeamClient`.
