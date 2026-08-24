# 分布式存储总览

AgentScope 通过 `IAgentStateStore` 接口抽象分布式状态存储，提供统一的 Get/Set/Delete 语义，并支持可选的版本化乐观并发。

## IAgentStateStore 接口

定义于 `AgentScope.Core.State`：

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

- `SupportsVersioning` — 指示后端是否支持版本化
- `GetVersionedAsync` — 获取状态及其当前版本号
- `SaveIfVersionAsync` — CAS（Compare-And-Swap）写入，仅在 `expectedVersion` 匹配时成功，返回新版本号

## 后端矩阵

| 后端 | 包名 | 构造方式 | 版本化支持 | 适用场景 |
|------|------|----------|:---------:|---------|
| **Redis** | `AgentScope.Extensions.Store.Redis` | `RedisAgentStateStore(RedisDistributedStore)` / `RedisAgentStateStore(connectionString)` | ✅ | 多副本生产，低延迟 |
| **MySQL** | `AgentScope.Extensions.Store.MySql` | `MySqlAgentStateStore(MySqlDistributedStore)` | ✅ | 已有 MySQL 基础设施 |
| **PostgreSQL** | `AgentScope.Extensions.Store.PostgreSql` | `PostgreSqlAgentStateStore(PostgreSqlDistributedStore)` | ✅ | 需 PostgreSQL 特性 |
| **OSS** | `AgentScope.Extensions.Store.Oss` | `OssAgentStateStore(OssDistributedStore)` | ❌ | 阿里云生态，大容量 |
| **COS** | `AgentScope.Extensions.Store.Cos` | `CosAgentStateStore(CosStore)` | ❌ | 腾讯云生态 |

## IDistributedStore 底层接口

所有 `*DistributedStore` 实现 `IDistributedStore`：

- `GetAsync(string key, CancellationToken ct = default)` → `ValueTask<string?>` — 获取原始数据
- `SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)` — 设置原始数据（支持 TTL 过期）
- `DeleteAsync(string key, CancellationToken ct = default)` → `ValueTask<bool>` — 删除
- `ListKeysAsync(string prefix, CancellationToken ct = default)` → `IAsyncEnumerable<string>` — 按前缀列举

## 如何选型

1. **低延迟、多副本** → **Redis**（版本化支持，生产首选）
2. **已有 MySQL/PostgreSQL** → **MySQL/PostgreSQL**（版本化支持，可共用数据库）
3. **阿里云/腾讯云生态、大容量归档** → **OSS/COS**（不支持版本化，last-writer-wins）
4. **本地开发调试** → `InMemoryAgentStateStore` 或 `JsonFileAgentStateStore`

## 结合 StateBackedMemory

任何 `IAgentStateStore` 都可与 `StateBackedMemory` 配合使用：

```csharp
var stateStore = new RedisAgentStateStore("redis://localhost:6379");
var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);
```

## 详细文档

- [Redis 后端](redis.md) — 连接串格式、构造方式、生产建议
- [MySQL 后端](mysql.md) — 连接串格式、构造方式
- [OSS 后端](oss.md) — 阿里云 OSS 接入
- [会话状态集成](../session/index.md) — SessionManager 与状态持久化用法
