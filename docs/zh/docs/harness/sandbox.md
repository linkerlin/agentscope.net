---
title: "沙箱"
description: "SandboxBase、WorkspaceSpec、生命周期与外部沙箱扩展"
---

## 概述

沙箱（`AgentScope.Harness.Sandbox`）为工具执行提供隔离环境：本地子进程、Docker 容器或远端沙箱服务。核心抽象：

```csharp
public abstract class SandboxBase : IAsyncDisposable
{
    // 子类必须实现
    protected abstract string WorkspaceRoot { get; }
    public abstract Task<ExecResult> ExecAsync(string command, int? timeoutSeconds = null, CancellationToken ct = default);
    public abstract Task<Stream> PersistWorkspaceAsync(CancellationToken ct = default);   // 打包工作区
    public abstract Task HydrateWorkspaceAsync(Stream archive, CancellationToken ct = default); // 恢复工作区
    public abstract Task StopAsync(CancellationToken ct = default);
    public abstract ValueTask DisposeAsync();

    // 基类模板方法：按 WorkspaceSpec + SandboxState 启动（4 分支）
    public Task StartAsync(WorkspaceSpec spec, SandboxState? state = null, CancellationToken ct = default);
}
```

`ExecResult(ExitCode, StdOut, StdErr, Truncated)` 为 `readonly record struct`。

## 启动分支

`StartAsync(spec, state)` 按输入走四条路径：

| 分支 | 条件 | 行为 |
|------|------|------|
| A | spec 有临时条目、无状态 | `ApplyEphemeralEntriesAsync` 只应用 Ephemeral 条目 |
| B | 有 `SandboxState`（恢复场景） | `RestoreSnapshotAsync` 从状态恢复 |
| C | 状态里带 `SnapshotRef` | `HydrateFromSnapshotAsync` + `ApplyAllEntriesAsync` |
| D | 全新沙箱 | `InitializeFromSpecAsync` 全量初始化 |

`WorkspaceSpecApplier.ApplyAsync(spec, targetDir, onlyEphemeral?)` 负责把条目实际落盘。

## WorkspaceSpec 与条目

```csharp
public sealed record WorkspaceSpec(
    string Root = "/workspace",
    IReadOnlyList<WorkspaceEntry>? Entries = null)
{
    public IReadOnlyDictionary<string, string> Environment { get; init; }
}
```

条目类型（`WorkspaceEntry(Path, Ephemeral)` 抽象基类）：

| 条目 | 字段 | 说明 |
|------|------|------|
| `FileEntry` | `Content` | 写入固定内容文件 |
| `DirEntry` | — | 创建目录 |
| `BindMountEntry`（Layout） | `Source`、`Target`、`ReadOnly` | 绑定挂载 |
| `LocalDirEntry` / `LocalFileEntry`（Layout） | `HostPath`、`ContainerPath` | 本地目录/文件映射 |
| `GitRepoEntry`（Layout） | `RepoUrl`、`Branch="main"`、`TargetPath?` | 克隆 Git 仓库 |
| `WorkspaceProjectionEntry`（Layout） | `SourceWorkspace`、`TargetPath` | 投影另一工作区 |

`Ephemeral = true` 的条目每次启动重建，不进入快照。

## 状态与租约

- `SandboxState(Id, SessionId, Spec, SnapshotRef, CreatedAt)`：可序列化的沙箱状态；
- `SandboxIsolationKey.Resolve(IsolationScope scope, RuntimeContext? ctx)`：按 Session / User 等维度生成隔离键；
- `SandboxLease(leaseId, sandboxId, ttl, onExpire?)`：租约（`Renew` / `Dispose` / `IsExpired`）；
- `SandboxManager(factory?, stateStore?, agentId?)`：`AcquireAsync(ctx)` 四级优先级获取沙箱，`ReleaseAsync` 释放，`PersistStateAsync` / `ClearStateAsync` 管理状态；
- `SessionSandboxStateStore`：随会话保存沙箱状态。

## DockerFilesystemSpec

`DockerFilesystemSpec(ContainerWorkspace = "/workspace", HostMountSource?, ReadOnly = false, UserId?)` 描述 Docker 容器挂载（`MountTarget` 属性即容器工作区路径）。

## 快照

`ISandboxSnapshot`（`Id` / `Type` / `IsPersistenceEnabled`）：`PersistAsync(Stream)` / `RestoreAsync()` / `IsRestorable()`。实现包括 `LocalSandboxSnapshot`、`NoopSandboxSnapshot`、`RemoteSandboxSnapshot`（经 `RemoteSnapshotClient`），分别由 `LocalSnapshotSpec` / `NoopSnapshotSpec` / `RemoteSnapshotSpec` 工厂构建。

## 中间件

`SandboxLifecycleMiddleware(SandboxManager? manager = null)`（Order 50，自动装配）在回合中注入/移除 `ctx.Items["sandbox"]` 上下文。

## 外部沙箱扩展

以下扩展项目实现 `AgentScope.Extensions.Sandbox.ISandbox` 接口（与 Harness `SandboxBase` 平行，经适配接入）：

| 扩展 | 构造 |
|------|------|
| `AgentScope.Extensions.Sandbox.Docker.DockerSandbox` | `(string image = "ubuntu:22.04", string? containerName = null)` |
| `AgentScope.Extensions.Sandbox.E2B.E2BSandbox` | `(HttpClient, string apiKey, string? baseUrl = null)` |
| `AgentScope.Extensions.Sandbox.Daytona.DaytonaSandbox` | `(HttpClient, string baseUrl)` |
| `AgentScope.Extensions.Sandbox.AgentRun.AgentRunSandbox` | `(HttpClient, string baseUrl)` |
| `AgentScope.Extensions.Sandbox.Kubernetes.KubernetesSandbox` | `(string image = "ubuntu:22.04", string? kubeConfigPath = null, string? ns = null)` |

## 相关文档

- [文件系统](./filesystem.md) —— `SandboxBackedFilesystem`
- [子 Agent](./subagent.md)
