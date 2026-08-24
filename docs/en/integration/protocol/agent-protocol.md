# Agent Protocol

AgentScope's `AgentScope.Harness` provides an [Agent Protocol](https://agentprotocol.ai/) client implementation for remote subagents to submit tasks over standard HTTP.

## When to use

- You want the Agent to be remotely scheduled like a cloud function.
- You need to expose a Harness subagent over HTTP for a parent harness to call.

## Key Classes

### AgentProtocolTaskClient

`AgentScope.Harness.Subagent.Tasks.AgentProtocolTaskClient` wraps Agent Protocol HTTP requests:

```csharp
using AgentScope.Harness.Subagent.Tasks;

var client = new AgentProtocolTaskClient();

// Submit a task
await client.SubmitTaskAsync(
    baseUrl: "http://remote-agent:8080",
    headers: null,
    taskId: "task-001",
    agentId: "researcher",
    input: "Latest tech news",
    context: null);

// Query status
var status = await client.GetStatusAsync(baseUrl, headers, "task-001");

// Wait for result (blocking)
var result = await client.WaitForResultAsync(baseUrl, headers, "task-001", timeoutSeconds: 30);

// Cancel
await client.CancelTaskAsync(baseUrl, headers, "task-001");

// Resume (HITL scenario)
await client.ResumeTaskAsync(baseUrl, headers, "task-001",
    new List<RemoteConfirmDecision> { ... });
```

### Constructor

| Class | Constructor |
| --- | --- |
| `AgentProtocolTaskClient` | `AgentProtocolTaskClient(HttpClient? http = null)` |

### AgentProtocolTransport

`AgentScope.Harness.Subagent.Tasks.AgentProtocolTransport` implements `IRemoteSubagentTransport` for internal use by the Harness remote subagent mechanism:

```csharp
var transport = new AgentProtocolTransport();
// or with a custom client
var transport = new AgentProtocolTransport(new AgentProtocolTaskClient());
```

| Method | Description |
| --- | --- |
| `SubmitAsync(RemoteTarget, string taskId, string agentId, string input, ...)` | Submit a task to a remote agent |
| `GetStatusAsync(RemoteTarget, string taskId, ...)` | Query task status |
| `WaitForResultAsync(RemoteTarget, string taskId, long timeoutSeconds, ...)` | Wait for task completion |
| `CancelAsync(RemoteTarget, string taskId, ...)` | Cancel a task |
| `ResumeAsync(RemoteTarget, string taskId, List<RemoteConfirmDecision>, ...)` | Resume a paused task |

## Protocol Layering

| Layer | Role |
| --- | --- |
| **AG-UI** | User-facing chat UI event stream (browser ↔ app) |
| **Agent Protocol** | Internal remote-subagent / task HTTP API (parent harness ↔ remote agent service) |
| **A2A** | External agent-to-agent interop |

> Note: The Agent Protocol client lives in `AgentScope.Harness`, not `AgentScope.Core`. The `AgentScope.Core.A2A` namespace and Agent Protocol are independent.
