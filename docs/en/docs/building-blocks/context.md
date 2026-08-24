---
title: "Context and AgentState"
description: "IMemory, AgentState, IAgentStateStore, and session restoration"
---

## Overview

The conversation context of `EnhancedReActAgent` is stored in `IMemory` (`AgentScope.Core.Memory`):

```csharp
public interface IMemory
{
    void Add(Msg message);
    List<Msg> GetAll();
    List<Msg> GetRecent(int count);
    void Clear();
    int Count();
    bool Delete(string messageId);
}
```

- Default implementation `MemoryBase`: in-process `List<Msg>` + lock, lost on restart.
- Replace with a persistent implementation via Builder's `Memory(IMemory)`.

## AgentState and State Store

`AgentState` (`AgentScope.Core.State`) is a snapshot of restorable state:

```csharp
public class AgentState(string sessionId, string? userId = null)
{
    public string SessionId { get; }
    public string? UserId { get; }
    public string Summary { get; set; }        // Compressed summary
    public List<Msg> Context { get; }          // Conversation history
    public string ReplyId { get; set; }
    public int CurIter { get; set; }           // Current iteration
    public List<Msg> ContextMutable { get; set; }   // Non-serialized writable view
}
```

`IAgentStateStore` addresses by `(userId, sessionId, key)` tuple, supporting optimistic concurrency (CAS):

```csharp
public interface IAgentStateStore
{
    bool SupportsVersioning { get; }
    Task<AgentState?> GetAsync(string userId, string sessionId, string key);
    Task<VersionedState<AgentState>?> GetVersionedAsync(string userId, string sessionId, string key);
    Task SaveAsync(string userId, string sessionId, string key, AgentState state);
    Task<long> SaveIfVersionAsync(string userId, string sessionId, string key, AgentState state, long expectedVersion);
}
```

### Built-in and Extension Implementations

| Implementation | Module | Description |
|---------------|--------|-------------|
| `InMemoryAgentStateStore` | `AgentScope.Core` | In-process dictionary, for testing |
| `JsonFileAgentStateStore(filePath)` | `AgentScope.Core` | Single JSON file persistence (note: parameter is a **file path**, not a directory) |
| `RedisAgentStateStore` | `AgentScope.Extensions.Store.Redis` | Wraps `RedisDistributedStore(connectionString)`, or convenient constructor `(connectionString, keyPrefix)` |
| `MySqlAgentStateStore` / `PostgreSqlAgentStateStore` / `OssAgentStateStore` / `CosAgentStateStore` | `AgentScope.Extensions.Store.*` | Wrap corresponding `*DistributedStore`, all implement `IAgentStateStore` |

## StateBackedMemory: Auto-Persisting Memory

`StateBackedMemory` automatically writes every `IMemory` change (`Add` / `Clear` / `Delete`) to `IAgentStateStore` (fire-and-forget serial persistence with CAS auto-retry):

```csharp
using AgentScope.Core.Memory;
using AgentScope.Core.State;

var store = new JsonFileAgentStateStore("agent-state.json");
var initial = new AgentState(sessionId: "demo-session", userId: "alice");

IMemory memory = new StateBackedMemory(store, initial, stateKey: "default");

EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(model)
    .Memory(memory)
    .Build();
```

## Session and IStateModule: Session-Level Save/Restore

`EnhancedReActAgent` implements `IStateModule`, using `Session` (`AgentScope.Core.Session`) as the carrier:

```csharp
public interface IStateModule
{
    void SaveTo(Session session, string sessionKey);
    void LoadFrom(Session session, string sessionKey);       // Throws InvalidOperationException if not found
    void LoadIfExists(Session session, string sessionKey);   // Silently returns if not found
}
```

Saved content (controlled by the Builder's `StatePersistence(...)` strategy, default `StatePersistence.All`):

- `AgentMetaState`: name + system prompt;
- Memory messages (`List<Msg>`, when `MemoryManaged`);
- `ToolkitState`: tool group activation state (when `ToolkitManaged`).

```csharp
using AgentScope.Core.Session;

var sessionManager = new SessionManager();
Session session = sessionManager.CreateSession(name: "demo", agentName: "my_agent");

await agent.CallAsync(userMsg);
agent.SaveTo(session, "main");

// New process: rebuild agent and restore
agent.LoadIfExists(session, "main");
```

`SessionManager` provides `CreateSession` / `GetSession` / `SwitchSession` / `GetAllSessions` / `PauseSession` / `ResumeSession` for in-process session management; `Session` contains `Id` / `Name` / `Status` (Active/Paused/Closed) / `Context` dictionary / `Metadata`.

:::{note}
`Session` and `SessionManager` are in-process objects; cross-process restoration requires externalizing `Session.Context` content to `IAgentStateStore` (e.g., `StateBackedMemory`) or distributed Store extensions.
:::

## Other Memory Implementations

| Implementation | Description |
|---------------|-------------|
| `SqliteMemory(databasePath)` | EF Core SQLite persistence; `SearchAsync(query, limit)` LIKE search; supports `BeginBatch()/EndBatch()` batching |
| `InMemoryLongTermMemory(mode, embedding?)` | `ILongTermMemory` implementation: `AddAsync(text, metadata?)` / `SearchAsync(query, topK)` / `SummarizeAsync()`; `LongTermMemoryMode.Plaintext/Semantic/Hybrid` |
| `AgentStateMemoryView(AgentState)` | Directly maps to a view of `state.Context` |

`ILongTermMemory` can be exposed to the model via the static `LongTermMemoryTools` utility (`StoreMemory` / `SearchMemory` / `GetMemoriesByTag` / `DeleteMemory`), or auto-archived after each reply using `StaticLongTermMemoryHook(ltm)`.

## Harness Memory System

`AgentScope.Harness.Memory` provides transcription and consolidation on top of Core memory:

- `SessionTranscriptWriter(logDir, sessionId)`: Writes `{sessionId}.jsonl` transcripts (messages / tool calls / tool results / compaction marks);
- `SessionTree(baseDir, sessionId)`: Dual-file (`.ctx.jsonl` + `.log.jsonl`) context tree;
- `MemoryFlushManager(config, writer)`: Flushes messages/tool events to transcripts;
- `MemoryConsolidator(config, sessionTree, compactor?)`: Periodically consolidates logs into long-term summaries;
- `ConversationCompactor(config?)`: Conversation compactor, see [Context Compaction](../harness/compaction.md).

See [Harness Memory](../harness/memory.md).

## Related Documentation

- [Agent](./agent.md) — Builder's Memory / StatePersistence configuration
- [Context Compaction](../harness/compaction.md)
- [Session Storage Integration](../../integration/session/index.md)
