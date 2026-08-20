# Bailian Memory

`AgentScope.Extensions.Mem.Bailian.BailianLongTermMemory` integrates with Alibaba Cloud Bailian memory service.

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Mem.Bailian" Version="2.0.1" />
</ItemGroup>
```

## Construction

```csharp
using AgentScope.Extensions.Mem.Bailian;

var client = new BailianLongTermMemory(
    new HttpClient(),
    apiKey: Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"),
    baseUrl: null // defaults to https://bailian.aliyuncs.com/api/v1
);
```

Constructor: `(HttpClient http, string apiKey, string? baseUrl = null)`

## Methods

```csharp
// Store memory
string memoryId = await client.StoreAsync(sessionId: "session_001", content: "User wants a 9am water reminder");

// Retrieve memories
List<string> memories = await client.RetrieveAsync(sessionId: "session_001", query: "reminder");
```

## Adapter example

```csharp
public class BailianMemoryAdapter : AgentScope.Core.Memory.ILongTermMemory
{
    private readonly BailianLongTermMemory _client;
    private readonly string _sessionId;

    public BailianMemoryAdapter(BailianLongTermMemory client, string sessionId)
    {
        _client = client;
        _sessionId = sessionId;
    }

    public async Task AddAsync(string text, Dictionary<string, object>? metadata = null)
        => await _client.StoreAsync(_sessionId, text);

    public async Task<List<string>> SearchAsync(string query, int topK = 5)
        => await _client.RetrieveAsync(_sessionId, query);

    public Task<string> SummarizeAsync() => Task.FromResult("");
}
```
