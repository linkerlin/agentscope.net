---
title: "Going to Production"
description: "Production deployment: model credentials, state storage, observability, and extension selection"
---

## Credential Management

All model API Keys are read from environment variables (or passed directly in the constructor):

```csharp
IModel model = new DashScopeModel(
    "qwen-plus",
    Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"));
```

In production, it is recommended to use a secret management system to inject environment variables; `DotNetEnv` (used in repository samples) is only suitable for local development.

## Single Node vs Multi-Replica

| Component | Single Node Dev | Multi-Replica Production |
|------|----------|------------|
| Model | Any model class | Same, model calls should go through a gateway/proxy for unified egress |
| Memory | `MemoryBase` / `SqliteMemory` | `StateBackedMemory` + distributed `IAgentStateStore` |
| Session state | `Session` + `JsonFileAgentStateStore` | `AgentScope.Extensions.Store.Redis` (or other Store extension) |
| Transcript/logs | Local JSONL | Object storage (`AgentScope.Extensions.Store.Oss`) or external log pipeline |
| Sandbox | Local/Docker | Remote sandbox (E2B / Daytona / AgentRun / Kubernetes) |

## Distributed State Storage

```csharp
using AgentScope.Extensions.Store.Redis;

// Convenience constructor
var stateStore = new RedisAgentStateStore("redis://redis.prod:6379", keyPrefix: "agentstate");

// Or wrap DistributedStore
var store = new RedisDistributedStore("redis://redis.prod:6379");
var stateStore2 = new RedisAgentStateStore(store);
```

Other backends are isomorphic: `MySqlAgentStateStore(MySqlDistributedStore)`, `PostgreSqlAgentStateStore(...)`, `OssAgentStateStore(OssDistributedStore)`, `CosAgentStateStore(CosStore)`.

Combined with `StateBackedMemory`, conversation can be restored across processes:

```csharp
var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);

EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(model)
    .Memory(memory)
    .Build();
```

## Session Recovery and Failover

- Stateless engine: any replica can serve any session, `(UserId, SessionId)` is specified by the caller;
- Graceful shutdown: `GracefulShutdownManager` (`AgentScope.Core.Shutdown`) + `GracefulShutdownMiddleware` handles in-flight requests, saves state, optional partial reasoning strategy (`PartialReasoningPolicy`);
- Interrupt control: `InterruptibleAgentBase` / `CancellationManager` supports call-level cancellation.

## Multi-Tenant Isolation

- Session slots: `RuntimeContext.UserId` / `SessionId` isolate conversations;
- Filesystem: `LocalFsMode` (`Sandboxed` anchors to root / `Rooted` whitelist) or `PathPolicy`;
- Sandbox: `SandboxIsolationKey.Resolve(IsolationScope, ctx)` isolates by Session / User dimension;
- Skills: `RuntimeContextSkillRepository(Func<RuntimeContext?, ISkillRepository>)` selects repository by tenant.

## Observability

Built-in Jsonl tracing:

```csharp
using AgentScope.Core.Tracing;

// TracerRegistry + JsonlTraceExporter, automatically records spans with Agent calls
```

Production recommendation: OpenTelemetry:

```csharp
// AgentScope.Tracing.OpenTelemetry
services.AddAgentScopeTracing(options =>
{
    options.OtlpEndpoint = "http://otel-collector:4318";
    options.EnableConsole = true;
});
```

`OtelTracingMiddleware` is an `IHarnessMiddleware`, automatically integrated into the `HarnessAgent` pipeline (or manually added via `WithMiddleware`).

## Performance and Resources

- Single instance serving multiple sessions: agent instance is reused, parallel by session;
- Context budget: `WorkspaceContextMiddleware(maxContextTokens)` (default 8000), `CompactionMiddleware(maxContextLength)` (default 4096) control prompt size;
- Large tool results: `WithToolResultEviction(ToolResultEvictionConfig)` offloads and truncates;
- Background tasks: `AgentScope.Harness.Subagent.Tasks` (`TaskRepository` / `BackgroundTask`) for async delegation.

## Common Deployment Modes

| Mode | Combination |
|------|------|
| Local CLI / Demo | `HarnessAgent` + MockModel + `WithWorkspaceRoot` |
| Web service (single node) | `HarnessAgent` + DashScope/OpenAI + `SqliteMemory` |
| Multi-replica service (production) | `EnhancedReActAgent` + Redis state + Docker/remote sandbox + OTel |
| Channel bot (DingTalk/Feishu/WeCom) | `AgentScope.Extensions.Channel.*` + Gateway |
