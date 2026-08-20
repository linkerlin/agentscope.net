# ReMe

`AgentScope.Extensions.Mem.ReMe.ReMeLongTermMemory` integrates with a self-hosted ReMe memory service.

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Mem.ReMe" Version="2.0.1" />
</ItemGroup>
```

## Construction

```csharp
using AgentScope.Extensions.Mem.ReMe;

var client = new ReMeLongTermMemory(
    new HttpClient(),
    baseUrl: "http://localhost:8002" // defaults to https://api.reme.ai/v1
);
```

Constructor: `(HttpClient http, string? baseUrl = null)`

## Methods

```csharp
// Save memory
string memoryId = await client.SaveAsync(userId: "workspace_001", memoryText: "User prefers quiet workspace");

// Query memories
List<string> memories = await client.QueryAsync(userId: "workspace_001", query: "workspace preference");
```

## Adapter example

```csharp
public class ReMeAdapter : AgentScope.Core.Memory.ILongTermMemory
{
    private readonly ReMeLongTermMemory _client;
    private readonly string _userId;

    public ReMeAdapter(ReMeLongTermMemory client, string userId)
    {
        _client = client;
        _userId = userId;
    }

    public async Task AddAsync(string text, Dictionary<string, object>? metadata = null)
        => await _client.SaveAsync(_userId, text);

    public async Task<List<string>> SearchAsync(string query, int topK = 5)
        => await _client.QueryAsync(_userId, query);

    public Task<string> SummarizeAsync() => Task.FromResult("");
}
```
