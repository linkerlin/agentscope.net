---
title: "Filesystem"
description: "IFilesystem abstraction: local / remote / sandbox / overlay / composite"
---

## Overview

`IFilesystem` (`AgentScope.Harness.Filesystem`) unifies Agent file operations and supports multiple deployment modes:

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

All results are `readonly record struct`: `ReadResult(Content?, Found, Error?)`, `WriteResult(Success, Error?)`, `EditResult(Success, Error?)`, `LsResult(Files, Error?)` (`FileInfo(Name, Path, IsDirectory, Size, LastModified)`), `GlobResult(Paths, Error?)`, `GrepResult(Matches, Error?)` (`GrepMatch(File, LineNumber, Line)`).

## Implementation Overview

| Implementation | Namespace | Scenario |
|------|----------|------|
| `LocalFilesystem(rootDir, mode, policy?)` | `...Filesystem.Local` | Local disk |
| `OverlayFilesystem(upper, lower)` | `...Filesystem` | Upper writable, lower read-only (copy-on-write) |
| `CompositeFilesystem(defaultBackend, routes?)` | `...Filesystem` | Route to different backends by path prefix |
| `BakedContextFilesystem(files?)` | `...Filesystem` | Pre-baked read-only context (`AddFile(path, content)`) |
| `RemoteFilesystem()` | `...Filesystem.Remote` | In-process KV storage (paired with remote storage protocol) |
| `SandboxBackedFilesystem(sandbox, id)` | `...Filesystem.Sandbox` | File operations via sandbox shell commands |

## Spec Builders

### LocalFilesystemSpec

```csharp
using AgentScope.Harness.Filesystem.Spec;

IFilesystem fs = new LocalFilesystemSpec()
    .WithRoot(".agentscope/workspace")        // required; optional projectRoot
    .WithMode(LocalFsMode.Sandboxed)          // Sandboxed (default) / Rooted / Unrestricted
    .WithPolicy(pathPolicy)                   // optional whitelist policy
    .WithProjectWritable(true)                // whether project root is writable
    .Build();                                 // returns OverlayFilesystem (R/W workspace + R/O project root)
```

`HarnessAgentBuilder.WithDefaultFilesystem(workspaceRoot?)` is a convenience wrapper around this.

### RemoteFilesystemSpec / SandboxFilesystemSpec

```csharp
// Remote shared storage (multi-replica shared workspace)
var spec = new RemoteFilesystemSpec(Endpoint: "store.internal:8080", Namespace: "team-a", Tls: true);

// Sandbox filesystem (start sandbox first)
// var (fs, sandboxCtx) = await new MySandboxFilesystemSpec().BuildAsync(hostWorkspaceRoot);
```

## LocalFsMode Isolation Levels

| Mode | Behavior |
|------|------|
| `Sandboxed` | All paths anchored to root, rejects `..` traversal (default, safest) |
| `Rooted` | Absolute paths only allowed within `PathPolicy` whitelist roots |
| `Unrestricted` | Absolute paths pass through as-is (dangerous, use only in controlled environments) |

## Sandbox Filesystem

`SandboxBackedFilesystem` inherits `SandboxFilesystemBase`, using shell commands for `ReadAsync` (cat/sed), `WriteAsync` (base64 decode write), `EditAsync` (sed), `ListAsync` (ls); `GlobAsync` / `GrepAsync` / `ExistsAsync` / `DeleteAsync` / `MoveAsync` throw `NotSupportedException`. It additionally provides `ExecuteAsync(command, timeout?)` for direct sandbox execution. See [Sandbox](./sandbox.md) for sandbox configuration.

## Integration with Agent

```csharp
HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithFilesystem(fs)                    // or WithDefaultFilesystem(".agentscope/workspace")
    .WithToolResultEviction(evictionCfg)   // eviction middleware will use this filesystem
    .Build();
```

The filesystem instance is also exposed to middlewares via `MiddlewareContext.Items["filesystem"]`. Harness's built-in `FilesystemTool` (`AgentScope.Harness.Tool`) wraps `IFilesystem` operations as model-invokable tools.

## Related Documentation

- [Sandbox](./sandbox.md)
- [Workspace](./workspace.md)
