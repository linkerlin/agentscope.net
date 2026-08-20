---
title: "上生产"
description: "生产部署：模型凭据、状态存储、可观测性与扩展选择"
---

## 凭据管理

所有模型 API Key 通过环境变量读取（或直接构造传入）：

```csharp
IModel model = new DashScopeModel(
    "qwen-plus",
    Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"));
```

生产环境建议使用密钥管理系统注入环境变量；`DotNetEnv`（仓库示例中使用）只适合本地开发。

## 单机 vs 多副本

| 组件 | 单机开发 | 多副本生产 |
|------|----------|------------|
| 模型 | 任意模型类 | 相同，模型调用建议走网关/代理统一出口 |
| 记忆 | `MemoryBase` / `SqliteMemory` | `StateBackedMemory` + 分布式 `IAgentStateStore` |
| 会话状态 | `Session` + `JsonFileAgentStateStore` | `AgentScope.Extensions.Store.Redis`（或其他 Store 扩展） |
| 转录/日志 | 本地 JSONL | 对象存储（`AgentScope.Extensions.Store.Oss`）或外部日志管线 |
| 沙箱 | 本地/Docker | 远端沙箱（E2B / Daytona / AgentRun / Kubernetes） |

## 分布式状态存储

```csharp
using AgentScope.Extensions.Store.Redis;

// 便捷构造
var stateStore = new RedisAgentStateStore("redis://redis.prod:6379", keyPrefix: "agentstate");

// 或包装 DistributedStore
var store = new RedisDistributedStore("redis://redis.prod:6379");
var stateStore2 = new RedisAgentStateStore(store);
```

其他后端同构：`MySqlAgentStateStore(MySqlDistributedStore)`、`PostgreSqlAgentStateStore(...)`、`OssAgentStateStore(OssDistributedStore)`、`CosAgentStateStore(CosStore)`。

结合 `StateBackedMemory` 即可跨进程恢复对话：

```csharp
var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);

EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Model(model)
    .Memory(memory)
    .Build();
```

## 会话恢复与故障转移

- 无状态引擎：任意副本可服务任意会话，`(UserId, SessionId)` 由调用方指定；
- 优雅停机：`GracefulShutdownManager`（`AgentScope.Core.Shutdown`）配合 `GracefulShutdownMiddleware` 处理在途请求、保存状态、可选部分推理策略（`PartialReasoningPolicy`）；
- 中断控制：`InterruptibleAgentBase` / `CancellationManager` 支持调用级取消。

## 多租户隔离

- 会话槽位：`RuntimeContext.UserId` / `SessionId` 隔离对话；
- 文件系统：`LocalFsMode`（`Sandboxed` 锚定根目录 / `Rooted` 白名单）或 `PathPolicy`；
- 沙箱：`SandboxIsolationKey.Resolve(IsolationScope, ctx)` 按 Session / User 维度隔离；
- 技能：`RuntimeContextSkillRepository(Func<RuntimeContext?, ISkillRepository>)` 按租户选仓库。

## 可观测性

内置 Jsonl 追踪：

```csharp
using AgentScope.Core.Tracing;

// TracerRegistry + JsonlTraceExporter，随 Agent 调用自动记录 span
```

生产推荐 OpenTelemetry：

```csharp
// AgentScope.Tracing.OpenTelemetry
services.AddAgentScopeTracing(options =>
{
    options.OtlpEndpoint = "http://otel-collector:4318";
    options.EnableConsole = true;
});
```

`OtelTracingMiddleware` 是 `IHarnessMiddleware`，自动纳入 `HarnessAgent` 管道（或手动 `WithMiddleware` 追加）。

## 性能与资源

- 单实例服务多会话：agent 实例复用，按会话并行；
- 上下文预算：`WorkspaceContextMiddleware(maxContextTokens)`（默认 8000）、`CompactionMiddleware(maxContextLength)`（默认 4096）控制 prompt 体量；
- 大工具结果：`WithToolResultEviction(ToolResultEvictionConfig)` 落盘截断；
- 后台任务：`AgentScope.Harness.Subagent.Tasks`（`TaskRepository` / `BackgroundTask`）异步委派。

## 常见部署形态

| 形态 | 组合 |
|------|------|
| 本地 CLI / 演示 | `HarnessAgent` + MockModel + `WithWorkspaceRoot` |
| Web 服务（单机） | `HarnessAgent` + DashScope/OpenAI + `SqliteMemory` |
| 多副本服务（生产） | `EnhancedReActAgent` + Redis 状态 + Docker/远端沙箱 + OTel |
| 渠道机器人（钉钉/飞书/企微） | `AgentScope.Extensions.Channel.*` + 网关 |
