# Mem0

`AgentScope.Extensions.Mem.Mem0.Mem0LongTermMemory` integrates with [Mem0](https://mem0.ai/).

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Mem.Mem0" Version="2.0.1" />
</ItemGroup>
```

## Construction

```csharp
using AgentScope.Extensions.Mem.Mem0;

var client = new Mem0LongTermMemory(
    new HttpClient(),
    apiKey: "your-mem0-api-key",
    baseUrl: null // defaults to https://api.mem0.ai/v1
);
```

Constructor: `(HttpClient http, string apiKey, string? baseUrl = null)`

## Methods

```csharp
// Store a memory
string memoryId = await client.AddAsync(userId: "user_123", agentId: "agent_456", message: "User prefers homestays");

// Search memories
List<string> memories = await client.SearchAsync(userId: "user_123", agentId: "agent_456", query: "accommodation preference");
```

## Adapter example

This class does not implement `ILongTermMemory`:

```csharp
public class Mem0Adapter : AgentScope.Core.Memory.ILongTermMemory
{
    private readonly Mem0LongTermMemory _client;
    private readonly string _userId, _agentId;

    public Mem0Adapter(Mem0LongTermMemory client, string userId, string agentId)
    {
        _client = client;
        _userId = userId;
        _agentId = agentId;
    }

    public async Task AddAsync(string text, Dictionary<string, object>? metadata = null)
        => await _client.AddAsync(_userId, _agentId, text);

    public async Task<List<string>> SearchAsync(string query, int topK = 5)
        => await _client.SearchAsync(_userId, _agentId, query);

    public Task<string> SummarizeAsync() => Task.FromResult("");
}
```
