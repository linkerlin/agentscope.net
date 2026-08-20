# AgentScope Studio

`AgentScope.Extensions.Studio` provides `AgentScopeStudioClient` for pushing Agent run records to [AgentScope Studio](https://github.com/agentscope-ai/agentscope-studio) for visual debugging and trace replay.

## When to use

- Inspect Agent conversations in Studio during development.
- Record production traces for retrospective analysis.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Studio" Version="2.0.1" />
```

## AgentScopeStudioClient

```csharp
using AgentScope.Extensions.Studio;

var client = new AgentScopeStudioClient(
    http: httpClient,
    baseUrl: "http://localhost:8000");

// Create a session
var sessionId = await client.CreateSessionAsync("agent-1");

// Log an event
await client.LogEventAsync(sessionId, "user_input", "Hello");

// Query the session
var session = await client.GetSessionAsync(sessionId);
```

### API

| Constructor | Description |
| --- | --- |
| `AgentScopeStudioClient(HttpClient http, string baseUrl)` | Connect to the Studio server |

| Method | Description |
| --- | --- |
| `CreateSessionAsync(string agentId, CancellationToken ct)` | Create a new session; returns `session_id` |
| `LogEventAsync(string sessionId, string type, string data, CancellationToken ct)` | Log an event to a session |
| `GetSessionAsync(string sessionId, CancellationToken ct)` | Get full session info (JSON format) |

> In production, consider gating Studio logging behind a configuration toggle to avoid unnecessary network overhead.
