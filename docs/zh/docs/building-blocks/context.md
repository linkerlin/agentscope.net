---
title: "上下文与 AgentState"
description: "IMemory、AgentState、IAgentStateStore 与会话恢复"
---

## 概述

`EnhancedReActAgent` 的对话上下文保存在 `IMemory`（`AgentScope.Core.Memory`）中：

```csharp
public interface IMemory
{
    void Add(Msg message);
    List<Msg> GetAll();
    List<Msg> GetRecent(int count);
    void Clear();
    int Count();
    bool Delete(string messageId);
}
```

- 默认实现 `MemoryBase`：进程内 `List<Msg>` + 锁，重启即失。
- 通过 Builder 的 `Memory(IMemory)` 替换为持久化实现。

## AgentState 与状态存储

`AgentState`（`AgentScope.Core.State`）是可恢复状态的快照：

```csharp
public class AgentState(string sessionId, string? userId = null)
{
    public string SessionId { get; }
    public string? UserId { get; }
    public string Summary { get; set; }        // 压缩摘要
    public List<Msg> Context { get; }          // 对话历史
    public string ReplyId { get; set; }
    public int CurIter { get; set; }           // 当前迭代
    public List<Msg> ContextMutable { get; set; }   // 不序列化的可写视图
}
```

`IAgentStateStore` 按 `(userId, sessionId, key)` 三元组寻址，支持乐观并发（CAS）：

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

### 内置与扩展实现

| 实现 | 模块 | 说明 |
|------|------|------|
| `InMemoryAgentStateStore` | `AgentScope.Core` | 进程内字典，测试用 |
| `JsonFileAgentStateStore(filePath)` | `AgentScope.Core` | 单 JSON 文件落盘（注意：参数是**文件路径**，不是目录） |
| `RedisAgentStateStore` | `AgentScope.Extensions.Store.Redis` | `RedisDistributedStore(connectionString)` 包装，或便捷构造 `(connectionString, keyPrefix)` |
| `MySqlAgentStateStore` / `PostgreSqlAgentStateStore` / `OssAgentStateStore` / `CosAgentStateStore` | `AgentScope.Extensions.Store.*` | 包装对应 `*DistributedStore`，均实现 `IAgentStateStore` |

## StateBackedMemory：自动持久化的记忆

`StateBackedMemory` 把 `IMemory` 的每次变更（`Add` / `Clear` / `Delete`）自动写入 `IAgentStateStore`（fire-and-forget 串行持久化，支持 CAS 自动重试）：

```csharp
using AgentScope.Core.Memory;
using AgentScope.Core.State;

var store = new JsonFileAgentStateStore("agent-state.json");
var initial = new AgentState(sessionId: "demo-session", userId: "alice");

IMemory memory = new StateBackedMemory(store, initial, stateKey: "default");

EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(model)
    .Memory(memory)
    .Build();
```

## Session 与 IStateModule：会话级保存/恢复

`EnhancedReActAgent` 实现 `IStateModule`，以 `Session`（`AgentScope.Core.Session`）为载体：

```csharp
public interface IStateModule
{
    void SaveTo(Session session, string sessionKey);
    void LoadFrom(Session session, string sessionKey);       // 不存在抛 InvalidOperationException
    void LoadIfExists(Session session, string sessionKey);   // 不存在静默返回
}
```

保存内容（受 Builder 的 `StatePersistence(...)` 策略控制，默认 `StatePersistence.All`）：

- `AgentMetaState`：名称 + 系统提示词；
- 记忆消息（`List<Msg>`，`MemoryManaged` 时）；
- `ToolkitState`：工具组激活状态（`ToolkitManaged` 时）。

```csharp
using AgentScope.Core.Session;

var sessionManager = new SessionManager();
Session session = sessionManager.CreateSession(name: "demo", agentName: "my_agent");

await agent.CallAsync(userMsg);
agent.SaveTo(session, "main");

// 新进程：重建 agent 后恢复
agent.LoadIfExists(session, "main");
```

`SessionManager` 提供 `CreateSession` / `GetSession` / `SwitchSession` / `GetAllSessions` / `PauseSession` / `ResumeSession` 等进程内会话管理；`Session` 本体含 `Id` / `Name` / `Status`（Active/Paused/Closed）/ `Context` 字典 / `Metadata`。

:::{note}
`Session` 与 `SessionManager` 是进程内对象；跨进程恢复需要把 `Session.Context` 的内容外置到 `IAgentStateStore`（如 `StateBackedMemory`）或分布式 Store 扩展。
:::

## 其他记忆实现

| 实现 | 说明 |
|------|------|
| `SqliteMemory(databasePath)` | EF Core SQLite 落盘；`SearchAsync(query, limit)` LIKE 检索；支持 `BeginBatch()/EndBatch()` 批量 |
| `InMemoryLongTermMemory(mode, embedding?)` | `ILongTermMemory` 实现：`AddAsync(text, metadata?)` / `SearchAsync(query, topK)` / `SummarizeAsync()`；`LongTermMemoryMode.Plaintext/Semantic/Hybrid` |
| `AgentStateMemoryView(AgentState)` | 直接映射到 `state.Context` 的视图 |

`ILongTermMemory` 可通过 `LongTermMemoryTools` 静态工具（`StoreMemory` / `SearchMemory` / `GetMemoriesByTag` / `DeleteMemory`）暴露给模型调用，也可用 `StaticLongTermMemoryHook(ltm)` 在每轮回复后自动归档。

## Harness 记忆体系

`AgentScope.Harness.Memory` 在 Core 记忆之上提供转录与整合：

- `SessionTranscriptWriter(logDir, sessionId)`：写 `{sessionId}.jsonl` 转录（消息 / 工具调用 / 工具结果 / 压缩标记）；
- `SessionTree(baseDir, sessionId)`：双文件（`.ctx.jsonl` + `.log.jsonl`）上下文树；
- `MemoryFlushManager(config, writer)`：把消息/工具事件刷写到转录；
- `MemoryConsolidator(config, sessionTree, compactor?)`：定期把日志整合为长期摘要；
- `ConversationCompactor(config?)`：对话压缩器，见[上下文压缩](../harness/compaction.md)。

详见 [Harness 记忆](../harness/memory.md)。

## 相关文档

- [智能体](./agent.md) —— Builder 的 Memory / StatePersistence 配置
- [上下文压缩](../harness/compaction.md)
- [会话存储集成](../../integration/session/index.md)
