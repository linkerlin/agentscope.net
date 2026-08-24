# 百炼记忆

`AgentScope.Extensions.Mem.Bailian.BailianLongTermMemory` 接入阿里云百炼记忆服务。

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Mem.Bailian" Version="2.0.1" />
</ItemGroup>
```

## 构造

```csharp
using AgentScope.Extensions.Mem.Bailian;

var client = new BailianLongTermMemory(
    new HttpClient(),
    apiKey: Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"),
    baseUrl: null // 默认 https://bailian.aliyuncs.com/api/v1
);
```

构造函数：`(HttpClient http, string apiKey, string? baseUrl = null)`

## 方法

```csharp
// 存储记忆
string memoryId = await client.StoreAsync(sessionId: "session_001", content: "用户希望每天 9 点提醒喝水");

// 检索记忆
List<string> memories = await client.RetrieveAsync(sessionId: "session_001", query: "提醒");
```

## 适配示例

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
