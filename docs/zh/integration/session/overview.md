# 会话与状态持久化概览

## Session 类

`Session` 定义于 `AgentScope.Core.Session`，表示一次对话会话：

```csharp
using AgentScope.Core.Session;

// id 可选，默认自动生成 GUID
Session session = new Session(name: "demo-session");
Console.WriteLine(session.Id); // auto-generated GUID
```

| 属性 | 类型 | 说明 |
|------|------|------|
| `Id` | `string` | 会话唯一标识（构造时可选，默认自动 GUID） |
| `Name` | `string?` | 会话名称，可选 |
| `Status` | `SessionStatus` | `Active` / `Paused` / `Closed` |

## SessionManager

`SessionManager` 提供会话的生命周期管理：

```csharp
using AgentScope.Core.Session;

var manager = new SessionManager();

// 创建会话
Session session = manager.CreateSession(name: "support-chat", agentName: "assistant");

// 获取会话
Session? existing = manager.GetSession(session.Id);

// 切换当前会话
manager.SwitchSession(session.Id);

// 列出所有会话
List<Session> all = manager.GetAllSessions();

// 暂停 / 恢复
manager.PauseSession(session.Id);
manager.ResumeSession(session.Id);

// 删除
manager.DeleteSession(session.Id);
```

`SessionStatus` 枚举值：
- `Active` — 会话活跃，可正常收发消息
- `Paused` — 已暂停，不处理新消息
- `Closed` — 已关闭

## 状态存储与 StateBackedMemory

`IAgentStateStore` 接口（`AgentScope.Core.State`）是状态持久化的核心抽象。内置实现：

- `InMemoryAgentStateStore` — 进程内，适用于单元测试
- `JsonFileAgentStateStore(string filePath)` — 单文件 JSON，适用于单机开发

### StateBackedMemory

`StateBackedMemory` 包装一个 `IAgentStateStore`，将内存变更自动同步到后端存储：

```csharp
using AgentScope.Core;
using AgentScope.Core.Memory;
using AgentScope.Core.State;

var stateStore = new InMemoryAgentStateStore();
var initial = new AgentState("demo-session", userId: "alice");

IMemory memory = new StateBackedMemory(
    stateStore,
    initial,
    stateKey: "default"     // 可选，默认 "default"
);
```

`StateBackedMemory` 属性：
- `State` — 当前 `AgentState`，包含 `SessionId`、`UserId`、`Summary`、`Context`（`List<Msg>`）、`ReplyId`、`CurIter`
- `LastPersistException` — 最近一次持久化异常（如有）

### AgentState

```csharp
using AgentScope.Core.State;

// sessionId 必填，userId 可选
var state = new AgentState("session-1", userId: "alice");

state.SessionId   // string
state.UserId      // string?
state.Summary     // string?
state.Context     // List<Msg>
state.ReplyId     // int?
state.CurIter     // int?
```

## EnhancedReActAgent 状态持久化

`EnhancedReActAgent`（`AgentScope.Core`）提供直接与会话交互的 Save/Load 方法：

```csharp
using AgentScope.Core;
using AgentScope.Core.Memory;
using AgentScope.Core.Model;
using AgentScope.Core.State;
using AgentScope.Core.Session;

// 1. 创建状态后端
var stateStore = new InMemoryAgentStateStore();
var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);

// 2. 构建 Agent
EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Name("assistant")
    .Model(new DashScopeModel("qwen-plus", apiKey))
    .Memory(memory)
    .Build();

// 3. 运行
await agent.CallAsync(Msg.Builder().Role("user").TextContent("你好").Build());

// 4. 保存到会话
var sessionManager = new SessionManager();
Session session = sessionManager.CreateSession(name: "demo");
agent.SaveTo(session, "main");

// 5. 从会话恢复（会话不存在则抛 InvalidOperationException）
agent.LoadFrom(session, "main");

// 安全恢复：不存在时静默跳过
agent.LoadIfExists(session, "main");
```

### SaveTo / LoadFrom 语义

| 方法 | 行为 |
|------|------|
| `SaveTo(Session, string sessionKey)` | 将当前 Agent 状态保存到指定会话的 key 下 |
| `LoadFrom(Session, string sessionKey)` | 从会话加载状态；key 不存在时抛 `InvalidOperationException` |
| `LoadIfExists(Session, string sessionKey)` | 安全加载；key 不存在时静默跳过 |

### StatePersistence

通过 builder 方法 `StatePersistence(StatePersistence)` 控制持久化范围：

```csharp
EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Name("assistant")
    .Model(new DashScopeModel("qwen-plus", apiKey))
    .Memory(memory)
    .StatePersistence(StatePersistence.MemoryManaged) // 仅持久化 Memory
    .Build();
```

`StatePersistence` 是一个 record，支持位组合：
- `StatePersistence.MemoryManaged` — 持久化 Memory
- `StatePersistence.ToolkitManaged` — 持久化 Toolkit
- `StatePersistence.PlanNotebookManaged` — 持久化 PlanNotebook
- `StatePersistence.All` — 三者全部（默认值）

## SqliteMemory

`SqliteMemory(string databasePath)` 基于 EF Core SQLite 的 `IMemory` 实现，适合本地持久化：

```csharp
using AgentScope.Core.Memory;

IMemory sqliteMemory = new SqliteMemory("./data/agent.db");
```

## IAgentStateStore 接口

完整接口定义（`AgentScope.Core.State`）：

```csharp
public interface IAgentStateStore
{
    bool SupportsVersioning { get; }

    Task<AgentState?> GetAsync(string userId, string sessionId, string key);
    Task<VersionedState<AgentState>?> GetVersionedAsync(string userId, string sessionId, string key);
    Task SaveAsync(string userId, string sessionId, string key, AgentState state);
    Task<long> SaveIfVersionAsync(string userId, string sessionId, string key, AgentState state, long expectedVersion);
}
```

分布式后端（Redis、MySQL、PostgreSQL、OSS、COS）详情请参阅[分布式存储文档](../distributed/index.md)。
