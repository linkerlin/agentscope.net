---
title: "文件系统"
description: "IFilesystem 抽象：本地 / 远程 / 沙箱 / 叠加 / 组合"
---

## 概述

`IFilesystem`（`AgentScope.Harness.Filesystem`）统一 Agent 的文件操作，支持多种部署形态：

```csharp
public interface IFilesystem
{
    Task<ReadResult>  ReadAsync(string filePath, int? offset = null, int? limit = null, CancellationToken ct = default);
    Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default);
    Task<EditResult>  EditAsync(string filePath, string oldString, string newString, bool replaceAll = false, CancellationToken ct = default);
    Task<LsResult>    ListAsync(string path, CancellationToken ct = default);
    Task<GlobResult>  GlobAsync(string pattern, string? path = null, CancellationToken ct = default);
    Task<GrepResult>  GrepAsync(string pattern, string? path = null, string? glob = null, CancellationToken ct = default);
    Task<bool>        ExistsAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    Task MoveAsync(string from, string to, CancellationToken ct = default);
}
```

结果均为 `readonly record struct`：`ReadResult(Content?, Found, Error?)`、`WriteResult(Success, Error?)`、`EditResult(Success, Error?)`、`LsResult(Files, Error?)`（`FileInfo(Name, Path, IsDirectory, Size, LastModified)`）、`GlobResult(Paths, Error?)`、`GrepResult(Matches, Error?)`（`GrepMatch(File, LineNumber, Line)`）。

## 实现一览

| 实现 | 命名空间 | 场景 |
|------|----------|------|
| `LocalFilesystem(rootDir, mode, policy?)` | `...Filesystem.Local` | 本机磁盘 |
| `OverlayFilesystem(upper, lower)` | `...Filesystem` | 上层可写、下层只读（写时复制） |
| `CompositeFilesystem(defaultBackend, routes?)` | `...Filesystem` | 按路径前缀路由到不同后端 |
| `BakedContextFilesystem(files?)` | `...Filesystem` | 预烘焙只读上下文（`AddFile(path, content)`） |
| `RemoteFilesystem()` | `...Filesystem.Remote` | 进程内 KV 存储（配合远端存储协议） |
| `SandboxBackedFilesystem(sandbox, id)` | `...Filesystem.Sandbox` | 通过沙箱 shell 执行文件操作 |

## Spec 构建器

### LocalFilesystemSpec

```csharp
using AgentScope.Harness.Filesystem.Spec;

IFilesystem fs = new LocalFilesystemSpec()
    .WithRoot(".agentscope/workspace")        // 必填；可选 projectRoot
    .WithMode(LocalFsMode.Sandboxed)          // Sandboxed（默认）/ Rooted / Unrestricted
    .WithPolicy(pathPolicy)                   // 可选白名单策略
    .WithProjectWritable(true)                // 项目根是否可写
    .Build();                                 // 返回 OverlayFilesystem（R/W 工作区 + R/O 项目根）
```

`HarnessAgentBuilder.WithDefaultFilesystem(workspaceRoot?)` 就是它的便捷封装。

### RemoteFilesystemSpec / SandboxFilesystemSpec

```csharp
// 远端共享存储（多副本共享工作区）
var spec = new RemoteFilesystemSpec(Endpoint: "store.internal:8080", Namespace: "team-a", Tls: true);

// 沙箱文件系统（先启动沙箱）
// var (fs, sandboxCtx) = await new MySandboxFilesystemSpec().BuildAsync(hostWorkspaceRoot);
```

## LocalFsMode 隔离级别

| 模式 | 行为 |
|------|------|
| `Sandboxed` | 所有路径锚定根目录，拒绝 `..` 遍历（默认，最安全） |
| `Rooted` | 绝对路径仅允许在 `PathPolicy` 白名单根目录内 |
| `Unrestricted` | 绝对路径原样通过（危险，仅受控环境使用） |

## 沙箱文件系统

`SandboxBackedFilesystem` 继承 `SandboxFilesystemBase`，用 shell 命令实现 `ReadAsync`（cat/sed）、`WriteAsync`（base64 解码写入）、`EditAsync`（sed）、`ListAsync`（ls）；`GlobAsync` / `GrepAsync` / `ExistsAsync` / `DeleteAsync` / `MoveAsync` 抛 `NotSupportedException`，额外提供 `ExecuteAsync(command, timeout?)` 直连沙箱执行。沙箱本身的配置见[沙箱](./sandbox.md)。

## 与 Agent 集成

```csharp
HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithFilesystem(fs)                    // 或 WithDefaultFilesystem(".agentscope/workspace")
    .WithToolResultEviction(evictionCfg)   // 驱逐中间件会使用该文件系统
    .Build();
```

文件系统实例同时通过 `MiddlewareContext.Items["filesystem"]` 暴露给中间件。Harness 内置的 `FilesystemTool`（`AgentScope.Harness.Tool`）把 `IFilesystem` 操作包装为模型可调用的工具。

## 相关文档

- [沙箱](./sandbox.md)
- [工作区](./workspace.md)
