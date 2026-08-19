---
title: "V1 Migration Guide"
description: "Complete migration guide from AgentScope .NET 1.x to 2.0"
---

:::{tip}
Looking for per-version change records? See [Release Notes](others/release-notes.md).
:::

AgentScope .NET 2.0 aims to preserve compatibility with 1.x where possible so that most users can upgrade smoothly. That said, 2.0 does introduce API-level changes. This page splits those changes into two sections:

- **Migration Guide** — what changes against 1.x, in two tiers:
  - **Part A · Required** — your code will fail to compile or throw at runtime if you don't migrate
  - **Part B · Recommended** — still works but `[Obsolete(forRemoval = true)]`; will be removed in the next minor
- **What's New** — net-new capabilities that don't appear in the Migration Guide

## Migration Guide

### Part A — Required (compile errors or runtime exceptions if you don't migrate)

Items in this section are removed, renamed, or have their semantics tightened. Code that worked on 1.x will not work as-is on 2.0.

#### A.1 Removed `ReActAgent.Builder` methods

| Removed in 2.0 | Replacement |
|---|---|
| `.Memory(Memory)` | `.StateStore(IAgentStateStore)` — `AgentState.Context` holds the conversation; the configured `IAgentStateStore` saves/loads automatically on every `CallAsync()`, keyed by the call's `(userId, sessionId)` from `RuntimeContext` |
| `.StatePersistence(StatePersistence)` | Same — `IAgentStateStore` subsumes persistence |
| `.StructuredOutputReminder(StructuredOutputReminder)` | No longer needed — structured output is now handled natively at the model layer (`Model.SupportsNativeStructuredOutput()`); the framework automatically selects native JSON schema or falls back to tool-choice |

Detail → [Context](building-blocks/context.md)

#### A.2 Removed packages and classes

| Removed in 2.0 | Replacement |
|---|---|
| `AgentScope.Core.Session.SessionManager` | Configure `.StateStore(IAgentStateStore)` on the agent builder; persistence happens automatically per `(userId, sessionId)` |
| `AgentScope.Core.Pipeline.*` — `Pipeline`, `Pipelines`, `SequentialPipeline`, `FanoutPipeline`, `MsgHub` | Compose middleware + sub-agents + the event stream for multi-agent orchestration. See the subagent guide → [Subagent](harness/subagent.md) |
| `AgentScope.Core.Model.Tts.*` | Core no longer ships TTS. Integrate the upstream provider SDK directly if you need TTS |
| `AgentScope.Core.Model.StructuredOutputReminder` | No longer needed — structured output is handled natively at the model layer |
| `AgentScope.Core.Agent.StructuredOutputCapableAgent` | Removed — structured output capability is inlined into `ReActAgent` with native model-layer support |
| `AgentScope.Core.Hook.PendingToolRecoveryHook` | Use `Builder.EnablePendingToolRecovery(bool)` |
| `AgentScope.Core.Hook.TtsHook` | Removed alongside the TTS module |

#### A.3 Model providers moved out of core

OpenAI, Gemini, Anthropic, DashScope, and Ollama chat model implementations are no longer packaged in `AgentScope.Core`. Core now keeps only shared model contracts such as `IModel`, `ChatModelBase`, `Formatter`, `ModelRegistry`, and the `IModelProvider` SPI.

If your v1 code imported provider classes from core, replace them with the matching model extension package:

| v1 import / dependency | v2 replacement |
|---|---|
| `AgentScope.Core.Model.OpenAIChatModel` | Add `AgentScope.Extensions.Model.OpenAI`; using `AgentScope.Extensions.Model.OpenAI.OpenAIChatModel` |
| `AgentScope.Core.Model.GeminiChatModel` | Add `AgentScope.Extensions.Model.Gemini`; using `AgentScope.Extensions.Model.Gemini.GeminiChatModel` |
| `AgentScope.Core.Model.AnthropicChatModel` | Add `AgentScope.Extensions.Model.Anthropic`; using `AgentScope.Extensions.Model.Anthropic.AnthropicChatModel` |
| `AgentScope.Core.Model.DashScopeChatModel` | Add `AgentScope.Extensions.Model.DashScope`; using `AgentScope.Extensions.Model.DashScope.DashScopeChatModel` |
| `AgentScope.Core.Model.OllamaChatModel` | Add `AgentScope.Extensions.Model.Ollama`; using `AgentScope.Extensions.Model.Ollama.OllamaChatModel` |
| `AgentScope.Core.Formatter.<provider>.*` | `AgentScope.Extensions.Model.<provider>.Formatter.*` |
| `AgentScope.Core.Credential.<Provider>Credential` | `AgentScope.Extensions.Model.<provider>.Credential.<Provider>Credential` |

`ModelRegistry` string ids still work, but only when the matching extension package is on the classpath:

```csharp
ReActAgent agent = ReActAgent.CreateBuilder()
    .Name("assistant")
    .Model("dashscope:qwen-plus")
    .Build();
```

ASP.NET Core applications should use the provider-specific extensions instead of relying on a generic core model path:

| Provider | NuGet package |
|---|---|
| OpenAI | `AgentScope.Extensions.Model.OpenAI` |
| DashScope | `AgentScope.Extensions.Model.DashScope` |
| Gemini | `AgentScope.Extensions.Model.Gemini` |
| Anthropic | `AgentScope.Extensions.Model.Anthropic` |
| Ollama | `AgentScope.Extensions.Model.Ollama` |

Detail → [Model](building-blocks/model.md), [Model Providers](../integration/overview.md)

#### A.4 `state` package restructure (compile error)

| v1 | v2 |
|---|---|
| `AgentMetaState` | `AgentState` |
| `StateModule` | **removed** — no longer a superclass for `Memory`, `Toolkit`, etc. |
| `StatePersistence` | **removed** — replaced by the `IAgentStateStore` abstraction |
| `ToolkitState` | Moved to `AgentScope.Core.State.Legacy.ToolkitState` (kept for compatibility only — do not reference in new code) |
| (new) | `Task`, `TaskContextState`, `ToolContextState`, `PlanModeContextState`, `ReadCacheEntry` |

Any code that imports `AgentMetaState`, `StateModule`, `StatePersistence`, or `ToolkitState` from `AgentScope.Core.State` will fail to compile. Detail → [Context](building-blocks/context.md)

#### A.5 `PlanNotebook` removed — use `HarnessAgent.EnablePlanMode()`

The entire `AgentScope.Core.Plan` namespace (`PlanNotebook`, `Plan`, `SubTask`, `PlanStorage`, `PlanToHint`, and related classes) has been removed with no deprecated bridge.

**What changed**: `PlanNotebook` modeled plans as structured `Plan` + `SubTask` objects with a state machine (todo → in_progress → done → abandoned) and 8 tool functions. The v2 replacement is a fundamentally different design — plan mode is now a **read-only investigation phase** where the agent designs an approach in a plain markdown file before gaining write access.

| v1 `PlanNotebook` | v2 Plan Mode |
|---|---|
| `ReActAgent.CreateBuilder().PlanNotebook(PlanNotebook.CreateBuilder().Build())` | `HarnessAgent.CreateBuilder().EnablePlanMode()` |
| Structured `Plan` + `SubTask` objects with state machine | Plain markdown file (`plans/PLAN.md`) |
| 8 tools: `CreatePlan`, `ReviseCurrentPlan`, `UpdateSubtaskState`, `FinishSubtask`, `FinishPlan`, `ViewSubtasks`, `ViewHistoricalPlans`, `RecoverHistoricalPlan` | 3 tools: `plan_enter`, `plan_write`, `plan_exit` |
| Plan and execution intermixed — no read-only restriction | Plan mode is read-only; `plan_exit` triggers HITL gate before the agent regains write access |
| `PlanToHint` injected contextual hints per reasoning step | `PlanModeMiddleware` blocks mutating tools while in plan mode |
| `PlanStorage` (in-memory) + `StateModule` persistence | Plan file written via `WorkspaceManager`; state in `AgentState.PlanModeContext` |

**Subtask tracking**: if your v1 code relied on `PlanNotebook`'s subtask state tracking (breaking work into subtasks and checking them off during execution), the v2 equivalent is the **task list** — enable it with `.EnableTaskList(true)` on the builder, which registers `TodoTools` and `TaskReminderMiddleware`.

#### A.6 `Msg` content validation is stricter (runtime exception)

`Msg` now validates `content` against `role` at construction time:

- `User` — only `TextBlock` / `DataBlock` / `ImageBlock` / `AudioBlock` / `VideoBlock`
- `System` — only `TextBlock`
- `Assistant` — unrestricted

Combinations that v1 tolerated (for example, a `User` message carrying a `ToolUseBlock`) now throw at construction. Use the role-pinned subclasses `UserMessage` / `AssistantMessage` / `SystemMessage` / `ToolResultMessage` to make role/content compatibility obvious at the call site. Detail → [Message & Event](building-blocks/message-and-event.md)

#### A.7 Agent is fully stateless (architecture change)

`ReActAgent` is now **fully stateless** — the instance itself holds no mutable "current session" state. All per-call mutable state (`AgentState`, `PermissionEngine`, event sink) is encapsulated in an internal `CallExecution` object and propagated through the call chain. A single Agent instance can safely serve multiple `(userId, sessionId)` combinations concurrently without cross-session interference.

**v1 → v2 impact**:

| Removed | Replacement |
|---|---|
| `ReActAgent.GetCurrentSessionId()` | Supplied via `RuntimeContext.SessionId` at `CallAsync()` time |
| `ReActAgent.GetCurrentUserId()` | Supplied via `RuntimeContext.UserId` at `CallAsync()` time |
| `AgentBase(name, desc, checkRunning, hooks)` constructor | Use `AgentBase(name, desc, hooks)` — `checkRunning` is no longer needed; concurrency is guaranteed by per-session serialization |
| `ReActAgent.GetState()` | `ReActAgent.GetAgentState()` or `GetAgentState(userId, sessionId)` |

`IsCheckRunning()` is still callable (returns `false`) and `Builder.CheckRunning(bool)` is still callable (ignored) — both are `[Obsolete]`.

---

### Part B — Recommended (`[Obsolete(forRemoval = true)]`, still callable today)

Items in this section compile and run on 2.0, but each has been marked for removal in the next minor. Migrate at your own pace; we recommend doing it sooner rather than later.

#### B.1 `SkillBox` → skill repositories

- `SkillBox` (the class) and `Builder.SkillBox(SkillBox)` are both `[Obsolete(forRemoval = true, since = "2.0.0")]`.
- Recommended path: register one or more `IAgentSkillRepository` implementations (built-ins: `ClasspathSkillRepository`, `FileSystemSkillRepository`) via `Builder.SkillRepository(...)` / `.SkillRepositories(...)`. When at least one repository is registered, `DynamicSkillMiddleware` is auto-installed and rebuilds the skill prompt on every `CallAsync()`.
- Fine-grained filtering: `Builder.SkillFilter(ISkillFilter)`.

Detail → [Skill](harness/skill.md)

#### B.2 Hook → Middleware

The entire `AgentScope.Core.Hook` namespace — the `IHook` interface, `HookEvent`, `HookEventType`, and all `*Event` classes — is `[Obsolete(forRemoval = true, since = "2.0.0")]`. Existing imports still compile, and `Builder.Hook(...)` / `.Hooks(...)` are kept callable via `LegacyHookDispatcher` so v1 code does not break overnight. The recommended extension surface is now `AgentScope.Core.Middleware`:

- `MiddlewareBase` exposes five stages: the onion-shaped `OnAgent` / `OnReasoning` / `OnActing` / `OnModelCall`, and the pipeline-shaped `OnSystemPrompt`.
- Builder methods: `.Middleware(MiddlewareBase)` and `.Middlewares(List<MiddlewareBase>)`.
- Built-in: `TaskReminderMiddleware` (pairs with `TodoTools`, re-injects the task list before each reasoning step).

Detail → [Middleware](building-blocks/middleware.md)

#### B.3 `Memory` → `IAgentStateStore` + `AgentState`

- The `AgentScope.Core.Memory.IMemory` interface and every implementation (`InMemoryMemory`, `LongTermMemory`, …) are `[Obsolete(forRemoval = true, since = "2.0.0")]`.
- `IMemory` no longer inherits from `StateModule`. It gains `SaveTo(IAgentStateStore, userId, sessionId)` / `LoadFrom(IAgentStateStore, userId, sessionId)` as a bridge so existing implementations can still round-trip through an `IAgentStateStore`.
- Recommended model:
  - **Conversation history** lives on `AgentState.Context`.
  - **Persistence** uses the `IAgentStateStore` abstraction (built-in: `InMemoryAgentStateStore`, `JsonFileAgentStateStore`), partitioned by the `(userId, sessionId)` pair.
  - Builder chain: `.StateStore(IAgentStateStore)` — `AgentState` is saved/loaded automatically on every `CallAsync()`, keyed by the `(userId, sessionId)` carried on the call's `RuntimeContext`.

Detail → [Context](building-blocks/context.md)

#### B.4 Event subscription: hooks + chunk events → `StreamEventsAsync()`

Code that watched text or tool-call deltas via `IHook` + `*ChunkEvent` in v1 can migrate to `agent.StreamEventsAsync()`, which returns an `IObservable<AgentEvent>` covering 28 typed events across the full agent lifecycle and the HITL flow (`RequireUserConfirmEvent`, `RequireExternalExecutionEvent`, `UserConfirmResultEvent`, `ExternalExecutionResultEvent`, …).

Alongside the new event stream, the `Msg` refactor adds:

- `DataBlock` — unified multimodal block, accepts base64 or URL sources
- `HintBlock` — agent guidance / intermediate reasoning
- `ToolCallState` / `ToolResultState` on `ToolUseBlock` / `ToolResultBlock` — tool-call lifecycle
- `Id` field on every block — stable references across the stream

Detail → [Message & Event](building-blocks/message-and-event.md)

##### `Stream()` → `StreamEventsAsync()` (alignment with Python 2.0)

Python 2.0's `agent.reply_stream()` exposes a single streaming signature (`AsyncGenerator[AgentEvent, None]`) that maps directly to .NET's fine-grained `AgentScope.Core.Event.AgentEvent` hierarchy. To match it, the coarse-grained `IObservable<Event> Stream(...)` API on the .NET side is `[Obsolete]` as of 2.0.0:

- **Methods (`forRemoval = true`, going away next minor)**
  - `IStreamableAgent.Stream(...)` — all overloads on the interface
  - `AgentBase.Stream(...)` — `IObservable<Event>` implementations
  - `ReActAgent.Stream(..., RuntimeContext)` — overloads
  - `HarnessAgent.Stream(...)` — all overloads. `HarnessAgent` gains new `StreamEventsAsync(Msg/List<Msg>[, RuntimeContext])` methods that delegate to `ReActAgent.StreamEventsAsync(...)` while reusing the sandbox lifecycle `AcquireForCall` / `ReleaseForCall`
  - `ReActAgent.StreamEventsAsync(..., RuntimeContext)` added — mirrors `CallAsync(..., RuntimeContext)` for context propagation
- **Types (soft deprecation, no `forRemoval` yet)**
  - `AgentScope.Core.Agent.Event`, `EventType`, `EventSource`
  - Still consumed internally by the harness (subagent event forwarding: `SubAgentTool` / `SubagentEventBus` / `DefaultAgentManager` / `AgentSpawnTool`), AGUI, A2A, chat-completions-web modules as the event-bus / adapter input. They will be flipped to `forRemoval = true` only after those modules migrate to `AgentEvent`, so the entire downstream is not warning-flooded in a single release.
  - Subagent events are forwarded on `HarnessAgent.StreamEventsAsync(...)` with a non-null `source` path (including remote Agent Protocol children when `remoteStreaming` is enabled).

New code should use:

```csharp
agent.StreamEventsAsync(new UserMessage("Hello"))
        .Subscribe(event =>
        {
            if (event.Type == AgentEventType.TextBlockDelta)
            {
                Console.Write(((TextBlockDeltaEvent)event).Delta);
            }
        });
```

#### B.5 RAG module — in progress

- `Knowledge`, `KnowledgeRetrievalTools`, `RAGMode`, `GenericRAGHook` are all `[Obsolete(forRemoval = true, since = "2.0.0")]`.
- The builder methods `.Knowledge(...)` / `.Knowledges(...)` / `.RagMode(...)` / `.RetrieveConfig(...)` are deprecated in parallel.
- The v2 rewrite is underway. New knowledge base, document reader, and store APIs will land in subsequent minor releases. The v1 implementations remain callable in 2.0 for compatibility, but **new code should not depend on them**.

#### B.6 Long-term memory module — in progress

- `LongTermMemory`, `LongTermMemoryMode`, `LongTermMemoryTools` are all `[Obsolete(forRemoval = true, since = "2.0.0")]`.
- The builder methods `.LongTermMemory(...)` / `.LongTermMemoryMode(...)` / `.LongTermMemoryAsyncRecord(...)` are deprecated in parallel.
- Same status — being rewritten on the v2 architecture. New code should not depend on the current API.

#### B.7 Core shell / file tools — no longer deprecated

- `AgentScope.Core.Tool.Coding.*` (`ShellCommandTool`, `CommandValidator`, `UnixCommandValidator`, `WindowsCommandValidator`) and `AgentScope.Core.Tool.File.*` (`ReadFileTool`, `WriteFileTool`, `FileToolUtils`) are **no longer `[Obsolete]`** as of 2.0.0-RC1.
- These tools run commands and read/write files directly against the host process. For `ReActAgent` users who don't need workspace / sandbox isolation, they are the recommended way to give the agent shell and file access:

```csharp
Toolkit toolkit = new Toolkit();
toolkit.RegisterTool(new ReadFileTool("/path/to/base/dir"));
toolkit.RegisterTool(new WriteFileTool("/path/to/base/dir"));
toolkit.RegisterTool(new ShellCommandTool());

ReActAgent agent = ReActAgent.CreateBuilder()
    .Toolkit(toolkit)
    /* ... */
    .Build();
```

- For `HarnessAgent` users, the harness module provides its own workspace-aware file and shell tools (`read_file`, `write_file`, `execute`, etc.) with unified local / Docker / cloud-sandbox stores, permission isolation, read/write cache, and HITL approval. It is recommended to use the built-in harness tools for workspace-integrated scenarios.

Detail → [Harness filesystem](harness/filesystem.md)

---

## What's New

The capabilities below are additive in 2.0 — none of them break 1.x code. The Migration Guide above already covers the event system, message refactor, and middleware mechanism, so they are not repeated here.

### AG-UI v2

- The AG-UI adapter now uses the v2 `StreamEventsAsync()` path. Normal `RunStarted` / `RunFinished` events are converted from `AgentStartEvent` / `AgentEndEvent`; error paths emit `RunError` and a fallback `RunFinished`.
- New `AgentEventConverter` and `AguiEventEnricher` extension points: converters handle semantic mapping, while enrichers handle cross-cutting properties such as `Timestamp` / `RawEvent`. The ASP.NET Core integration automatically collects both bean types.
- Every `AguiEvent` supports AG-UI base event properties. `BaseEventPropertiesEnricher` is disabled by default; when explicitly enabled, it only fills missing `Timestamp` values and does not default `RawEvent`.
- `AguiAdapterConfig.EmitTokenUsage` can emit `Custom token_usage` events with model-call delta and run-level cumulative token usage.
- **Behavior change:** AgentEvents with `source != null` (subagent events) are emitted as AG-UI `Custom` events (`subagent.lifecycle`, `subagent.text`, `subagent.thinking`, `subagent.tool_call`, `subagent.tool_result`, `subagent.require_confirm`) instead of native `TextMessage*` / `Run*`. Set `EmitSubagentEventsAsNative(true)` to restore the legacy native mapping.
- The ASP.NET Core integration supports `AguiRuntimeContextResolver`, custom `IAguiAgentAdapterFactory`, frontend tool injection / merge mode, and HITL interrupt output.

Detail → [AG-UI](../integration/protocol/agui.md)

### Toolkit & Permission

Tool execution is the main extension surface in 2.0, and the permission system sits directly on its execution path — so we present them together.

- **Toolkit upgrades**:
  - Unified base classes: `ToolBase` / `AgentTool`
  - Tool groups: `ToolGroup` / `ToolGroupScope` / `MetaToolFactory` — activate on demand; the reserved `basic` group is always on
  - Annotation-driven registration: `ReflectiveFunctionTool` + `[Tool]` / `[ToolParam]`; `Toolkit.RegisterTool(object)` reflectively registers any annotated methods
  - Built-in task tool: `AgentScope.Core.Tool.BuiltIn.TodoTools.TodoWrite` (pairs with `TaskReminderMiddleware`)
- **Permission system** (new namespace `AgentScope.Core.Permission`):
  - `PermissionEngine`, `PermissionRule`, `PermissionMode` (`Default` / `AcceptEdits` / `Explore` / `Bypass` / `DontAsk`), `PermissionBehavior`
  - Every tool call goes through `PermissionEngine`: allow / require user confirmation / deny. HITL decisions flow back as `UserConfirmResultEvent`.

Detail → [Tool](building-blocks/tool.md), [Permission System](building-blocks/permission-system.md)

### Model fault tolerance and credentials

- New namespace `AgentScope.Core.Credential` — shared credential contracts and `ModelCard`; provider-specific credentials live with the model extension packages
- `ModelRegistry` resolves models from `"provider:model"` strings when the matching model extension package is on the classpath (e.g. `dashscope:qwen-max`, `openai:gpt-5`)
- Builder additions: `.Model(string)`, `.MaxRetries(int)`, `.FallbackModel(IModel)` / `.FallbackModel(string)`, `.StopOnReject(bool)` — primary-model failure auto-retries and falls back

Detail → [Model](building-blocks/model.md)

### Workspace (Harness module)

- Workspace abstraction unifies local filesystem, Docker, and E2B cloud sandbox execution behind a single interface
- Warm-up pool — pre-initialize execution environments in batches; useful for parallel RL rollouts

Detail → [Workspace](harness/workspace.md)

### Other new Builder methods

- `.EnableTaskList(...)` / `.EnableTaskList(bool)` — enable the built-in `TodoTools`
- `.PermissionContext(PermissionContextState)` — preload permission rules
- `ReActAgent.Builder.FromAgent(ReActAgent)` — derive a new builder from an existing agent's observable configuration (name, description, system prompt, model, maxIters, generateOptions, toolkit)
- `HarnessAgent.Builder.FromAgent(ReActAgent)` — ReActAgent → HarnessAgent migration helper. Inherits the same 7 fields as `ReActAgent.Builder.FromAgent` plus **every other observable configuration on ReActAgent**: `StateStore` / `DefaultSessionId`, `ModelConfig` (`MaxRetries` / `FallbackModel`), `ReactConfig.StopOnReject`, `ModelExecutionConfig` / `ToolExecutionConfig` / `ToolExecutionContext`, `EnablePendingToolRecovery`, `CheckRunning`, `PermissionContext`, `Middlewares`, and `Hooks`. The only flags not copied are `EnableMetaTool` / `EnableTaskList` — these are builder-time toolkit-mutation flags, and the toolkit copy already carries the tools they registered. Harness-only config (workspace / filesystem / subagents / skills / plan mode / `Disable*` toggles) still has to be set explicitly. See XML doc for the full table.
- **New getters on ReActAgent / parents to support the above migration**: `GetModelExecutionConfig()` / `GetToolExecutionConfig()` / `GetToolExecutionContext()` / `IsPendingToolRecoveryEnabled()` / `GetPermissionContext()` (on `ReActAgent`); `IsCheckRunning()` (on `AgentBase`, deprecated, always returns `false`).

Detail → [Agent](building-blocks/agent.md)

### Dedicated model for Memory / Compaction

`MemoryConfig` and `CompactionConfig` gain `.Model(IModel)` / `.Model(string)` builder methods, allowing a dedicated (typically lighter/cheaper) model for memory flush, consolidation, and context compaction operations independent of the agent's primary reasoning model. When not set, the agent's primary model is used (preserving existing behavior).

```csharp
HarnessAgent.CreateBuilder()
    .Model("openai:o3")
    .Memory(MemoryConfig.CreateBuilder()
        .Model("openai:gpt-4.1-mini")
        .Build())
    .Compaction(CompactionConfig.CreateBuilder()
        .Model("openai:gpt-4.1-mini")
        .Build())
    .Build();
```
