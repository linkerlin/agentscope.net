# ReMe

`AgentScope.Extensions.Mem.ReMe.ReMeLongTermMemory` 接入自托管 ReMe 记忆服务。

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Mem.ReMe" Version="2.0.1" />
</ItemGroup>
```

## 构造

```csharp
using AgentScope.Extensions.Mem.ReMe;

var client = new ReMeLongTermMemory(
    new HttpClient(),
    baseUrl: "http://localhost:8002" // 默认 https://api.reme.ai/v1
);
```

构造函数：`(HttpClient http, string? baseUrl = null)`

## 方法

```csharp
// 保存记忆
string memoryId = await client.SaveAsync(userId: "workspace_001", memoryText: "用户偏好安静的工作环境");

// 查询记忆
List<string> memories = await client.QueryAsync(userId: "workspace_001", query: "工作环境偏好");
```

## 适配示例

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
