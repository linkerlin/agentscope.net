# AgentScope Studio

`AgentScope.Extensions.Studio` 提供 `AgentScopeStudioClient`，用于将 Agent 运行记录推送到 [AgentScope Studio](https://github.com/agentscope-ai/agentscope-studio)，实现可视化调试与链路回放。

## 何时使用

- 开发期想在 Studio 中查看 Agent 的会话记录。
- 需要将生产流量录制到 Studio 进行追溯分析。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Studio" Version="2.0.1" />
```

## AgentScopeStudioClient

```csharp
using AgentScope.Extensions.Studio;

var client = new AgentScopeStudioClient(
    http: httpClient,
    baseUrl: "http://localhost:8000");

// 创建会话
var sessionId = await client.CreateSessionAsync("agent-1");

// 记录事件
await client.LogEventAsync(sessionId, "user_input", "你好");

// 查询会话
var session = await client.GetSessionAsync(sessionId);
```

### API

| 构造方法 | 说明 |
| --- | --- |
| `AgentScopeStudioClient(HttpClient http, string baseUrl)` | 连接 Studio 服务端 |

| 方法 | 说明 |
| --- | --- |
| `CreateSessionAsync(string agentId, CancellationToken ct)` | 创建新会话，返回 `session_id` |
| `LogEventAsync(string sessionId, string type, string data, CancellationToken ct)` | 向会话写入事件 |
| `GetSessionAsync(string sessionId, CancellationToken ct)` | 获取会话完整信息（JSON 格式） |

## 工作原理

1. 每次 Agent 调用前调用 `CreateSessionAsync` 创建会话。
2. 调用过程中通过 `LogEventAsync` 记录消息、工具调用等事件。
3. 开发者在 Studio 前端查看会话时间线。

> 生产环境建议通过配置开关控制是否启用 Studio 日志，避免不必要的网络开销。
