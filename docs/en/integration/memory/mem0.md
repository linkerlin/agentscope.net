# Mem0

`agentscope-extensions-mem0` integrates [Mem0](https://mem0.ai/) as the long-term memory store, combining vector search with LLM-based memory extraction. It supports the Mem0 SaaS platform, self-hosted deployments, and local stand-alone setups.

## When to use

- You want cross-session factual memory in your Agent (user preferences, prior decisions).
- You need multi-tenant isolation via three metadata layers: `agentId / userId / runId`.
- You want to filter memory at retrieval time using custom metadata, e.g. `category=travel`.

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Mem0" Version="$(AgentScopeVersion)" />
</ItemGroup>
```

## Quickstart

```csharp
using AgentScope.core.memory.mem0.Mem0LongTermMemory;
using AgentScope.core.memory.mem0.Mem0ApiType;
// 1. Build a memory instance (local, no auth)
Mem0LongTermMemory memory = Mem0LongTermMemory.Builder()
    .AgentName("Assistant")
    .UserId("user_123")
    .ApiBaseUrl("http://localhost:8000")
    .ApiType(Mem0ApiType.SELF_HOSTED)
    .Build();
// 2. Wire it into an Agent
ReActAgent agent = ReActAgent.Builder()
    .Name("Assistant")
    .Model(model)
    .LongTermMemory(memory)
    .LongTermMemoryMode(LongTermMemoryMode.BOTH)
    .Build();
// 3. Talk normally — memory is recorded/retrieved automatically
agent.Call(new UserMessage("I prefer homestays when traveling")).GetAwaiter().GetResult();
```

## Deployment modes

`Mem0ApiType` chooses the URL conventions and auth scheme:

| Enum | Use case | `apiBaseUrl` example | `apiKey` |
| --- | --- | --- | --- |
| `PLATFORM` (default) | Mem0 SaaS | `https://api.mem0.ai` | required |
| `SELF_HOSTED` | Self-hosted Mem0 server | `http://your-host:8000` | depends on deployment |

## Multi-tenant isolation

`agentName / userId / runName` are the three identifier layers Mem0 uses to organize memory:

```csharp
Mem0LongTermMemory memory = Mem0LongTermMemory.Builder()
    .AgentName("travel-bot")     // Agent-level memory shared across users
    .UserId("alice")             // User/tenant level
    .RunName("trip-2026-spring") // Per-session
    .ApiBaseUrl("http://localhost:8000")
    .Build();
```

At least one of the three must be provided; otherwise `build()` throws `IllegalArgumentException`. Retrieval only returns memories whose metadata matches.

## Custom metadata filtering

`metadata(...)` is applied to both writes and reads — it is persisted with each memory and injected as a filter when retrieving.

```csharp
var tags = new Dictionary<string, object>
{
    ["category"] = "travel",
    ["project_id"] = "proj_001"
};
Mem0LongTermMemory memory = Mem0LongTermMemory.Builder()
    .AgentName("Assistant")
    .UserId("user_123")
    .ApiBaseUrl("http://localhost:8000")
    .Metadata(tags)
    .Build();
// All record() calls now carry tags; retrieve() only matches memories with the same tags
```

Useful when slicing knowledge by project or business line. If you only need per-user separation, `userId` alone is enough.

## Builder reference

| Method | Required | Default | Notes |
| --- | --- | --- | --- |
| `apiBaseUrl(String)` | ✅ | - | Mem0 service URL |
| `apiKey(String)` | depends | - | API key; not needed for local unauthenticated deployments |
| `apiType(Mem0ApiType)` | ❌ | `PLATFORM` | Selects SaaS vs. self-hosted routing |
| `agentName(String)` | one of three | - | Agent-level ID |
| `userId(String)` | one of three | - | User-level ID |
| `runName(String)` | one of three | - | Run/session-level ID |
| `metadata(Map)` | ❌ | `null` | Extra filter applied on both writes and reads |
| `timeout(Duration)` | ❌ | `60s` | HTTP timeout |

> At least one of `agentName / userId / runName` must be set, otherwise `build()` throws `IllegalArgumentException`.
