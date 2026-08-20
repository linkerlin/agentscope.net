# Agent Protocol

AgentScope 在 `AgentScope.Harness` 中提供了 [Agent Protocol](https://agentprotocol.ai/) 客户端实现，用于远程子 agent 通过 HTTP 标准接口提交任务。

## 何时使用

- 想让 Agent 像云函数一样被远程调度。
- 需要将 Harness 子 agent 通过 HTTP 暴露给父 harness。

## 关键类

### AgentProtocolTaskClient（HTTP 客户端）

`AgentScope.Harness.Subagent.Tasks.AgentProtocolTaskClient` 封装了 Agent Protocol 的 HTTP 请求：

```csharp
using AgentScope.Harness.Subagent.Tasks;

var client = new AgentProtocolTaskClient();

// 提交任务
await client.SubmitTaskAsync(
    baseUrl: "http://remote-agent:8080",
    headers: null,
    taskId: "task-001",
    agentId: "researcher",
    input: "搜索最新技术动态",
    context: null);

// 查询状态
var status = await client.GetStatusAsync(
    baseUrl, headers, "task-001");

// 等待结果（阻塞）
var result = await client.WaitForResultAsync(
    baseUrl, headers, "task-001",
    timeoutSeconds: 30);

// 取消任务
await client.CancelTaskAsync(baseUrl, headers, "task-001");

// 恢复（HITL 场景）
await client.ResumeTaskAsync(baseUrl, headers, "task-001",
    new List<RemoteConfirmDecision> { ... });
```

### 构造方法

| 类 | 构造方法 |
| --- | --- |
| `AgentProtocolTaskClient` | `AgentProtocolTaskClient(HttpClient? http = null)` |

### AgentProtocolTransport

`AgentScope.Harness.Subagent.Tasks.AgentProtocolTransport` 实现 `IRemoteSubagentTransport`，在 Harness 远程子 agent 机制内部使用：

```csharp
var transport = new AgentProtocolTransport();
// 或传入自定义客户端
var transport = new AgentProtocolTransport(new AgentProtocolTaskClient());
```

| 方法 | 说明 |
| --- | --- |
| `SubmitAsync(RemoteTarget, string taskId, string agentId, string input, ...)` | 提交任务到远程 agent |
| `GetStatusAsync(RemoteTarget, string taskId, ...)` | 查询任务状态 |
| `WaitForResultAsync(RemoteTarget, string taskId, long timeoutSeconds, ...)` | 等待任务完成 |
| `CancelAsync(RemoteTarget, string taskId, ...)` | 取消任务 |
| `ResumeAsync(RemoteTarget, string taskId, List<RemoteConfirmDecision>, ...)` | 恢复暂停的任务 |

## 协议分层

| 层级 | 角色 |
| --- | --- |
| **AG-UI** | 面向用户的聊天 UI 事件流（浏览器 ↔ 应用） |
| **Agent Protocol** | 内部远程子 agent / 任务 HTTP API（父 harness ↔ 远程 agent 服务） |
| **A2A** | 外部 Agent 间互操作 |

> 请注意：Agent Protocol 客户端是 `AgentScope.Harness` 的一部分而非 `AgentScope.Core`。`AgentScope.Core.A2A` 命名空间下的类与 Agent Protocol 相互独立。
