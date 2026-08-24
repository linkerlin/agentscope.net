# Memory Overview

## Core Memory Interfaces

`ILongTermMemory` in `AgentScope.Core.Memory`:

```csharp
public interface ILongTermMemory
{
    Task AddAsync(string text, Dictionary<string, object>? metadata = null);
    Task<List<string>> SearchAsync(string query, int topK = 5);
    Task<string> SummarizeAsync();
}
```

### Built-in: InMemoryLongTermMemory

```csharp
public InMemoryLongTermMemory(
    LongTermMemoryMode mode = LongTermMemoryMode.Plaintext,
    IEmbeddingGenerator? embedding = null)

public enum LongTermMemoryMode { Plaintext, Semantic, Hybrid }
```

- `Plaintext`: substring matching.
- `Semantic`: cosine similarity via injected `IEmbeddingGenerator`.
- `Hybrid`: vector recall ∪ substring recall, deduplicated fusion.

### LongTermMemoryTools (static)

Exposes `ILongTermMemory` as LLM-callable tools:

```csharp
// StoreMemory(ILongTermMemory memory, string content, string? tags = null)
// SearchMemory(ILongTermMemory memory, string query, int topK = 5)
// GetMemoriesByTag(ILongTermMemory memory, string tag)
// DeleteMemory(ILongTermMemory memory, string memoryId)
```

Register into a `Toolkit` to let the LLM read/write memory autonomously.

### StaticLongTermMemoryHook

`StaticLongTermMemoryHook(ILongTermMemory)` auto-archives assistant responses:

```csharp
var hook = new StaticLongTermMemoryHook(memory);
await hook.OnAfterResponseAsync(responseMsg);
```

### Other Core Memory Types

- `SqliteMemory(string databasePath)`: implements `IPersistentMemory` (extends `IMemory`), provides `SearchAsync(string query, int limit = 10)`.
- `StateBackedMemory(IAgentStateStore store, AgentState initial, string stateKey = "default")`: implements `IMemory` via `IAgentStateStore`.
- `MemoryBase`: in-memory `IMemory` implementation.

## Adapting Third-Party Memory Clients

Mem0, ReMe, and Bailian extension packages **do not implement** `ILongTermMemory`. Wrap them manually:

```csharp
public class MyMem0Adapter : ILongTermMemory
{
    private readonly Mem0LongTermMemory _client;
    public MyMem0Adapter(Mem0LongTermMemory client) => _client = client;

    public async Task AddAsync(string text, Dictionary<string, object>? metadata = null)
    {
        var userId = metadata?.TryGetValue("user_id", out var u) == true ? u.ToString()! : "default";
        var agentId = metadata?.TryGetValue("agent_id", out var a) == true ? a.ToString()! : "default";
        await _client.AddAsync(userId, agentId, text);
    }

    public async Task<List<string>> SearchAsync(string query, int topK = 5) { /* ... */ }
    public Task<string> SummarizeAsync() => Task.FromResult("");
}
```

The wrapper can then be used with `LongTermMemoryTools` or directly plugged into an Agent.
