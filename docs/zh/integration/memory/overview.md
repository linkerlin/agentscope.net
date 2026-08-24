# 记忆概览

## Core 记忆接口

`ILongTermMemory` 定义在 `AgentScope.Core.Memory`：

```csharp
public interface ILongTermMemory
{
    Task AddAsync(string text, Dictionary<string, object>? metadata = null);
    Task<List<string>> SearchAsync(string query, int topK = 5);
    Task<string> SummarizeAsync();
}
```

### 内置实现：InMemoryLongTermMemory

```csharp
public InMemoryLongTermMemory(
    LongTermMemoryMode mode = LongTermMemoryMode.Plaintext,
    IEmbeddingGenerator? embedding = null)

public enum LongTermMemoryMode { Plaintext, Semantic, Hybrid }
```

- `Plaintext`：子串匹配。
- `Semantic`：注入 `IEmbeddingGenerator` 后使用向量余弦相似度。
- `Hybrid`：向量召回 ∪ 子串召回，去重融合。

### LongTermMemoryTools（静态工具类）

将 `ILongTermMemory` 暴露为模型可调用的工具：

```csharp
// StoreMemory(ILongTermMemory memory, string content, string? tags = null)
// SearchMemory(ILongTermMemory memory, string query, int topK = 5)
// GetMemoriesByTag(ILongTermMemory memory, string tag)
// DeleteMemory(ILongTermMemory memory, string memoryId)
```

注册到 `Toolkit` 即可让 LLM 自主读写记忆。

### StaticLongTermMemoryHook

`StaticLongTermMemoryHook(ILongTermMemory)` 自动将每次 Agent 响应的 Assistant 消息归档到 `ILongTermMemory`：

```csharp
var hook = new StaticLongTermMemoryHook(memory);
await hook.OnAfterResponseAsync(responseMsg);
```

### 其他 Core 记忆类型

- `SqliteMemory(string databasePath)`：实现 `IPersistentMemory`（扩展自 `IMemory`），提供 `SearchAsync(string query, int limit = 10)`。
- `StateBackedMemory(IAgentStateStore store, AgentState initial, string stateKey = "default")`：实现 `IMemory`，通过 `IAgentStateStore` 持久化。
- `MemoryBase`：`IMemory` 的纯内存实现。

## 适配第三方记忆客户端

Mem0、ReMe、百炼扩展包**不实现** `ILongTermMemory`，需自行包装：

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
    public Task<string> SummarizeAsync() => Task.FromResult(""); // 按需实现
}
```

包装后即可通过 `LongTermMemoryTools` 暴露为工具，或直接传入 Agent。
