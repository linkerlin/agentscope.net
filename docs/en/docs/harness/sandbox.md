---
title: "Sandbox"
description: "SandboxBase, WorkspaceSpec, lifecycle, and external sandbox extensions"
---

## Overview

The sandbox (`AgentScope.Harness.Sandbox`) provides an isolated environment for tool execution: local subprocess, Docker containers, or remote sandbox services. Core abstraction:

```csharp
public abstract class SandboxBase : IAsyncDisposable
{
    // Subclasses must implement
    protected abstract string WorkspaceRoot { get; }
    public abstract Task<ExecResult> ExecAsync(string command, int? timeoutSeconds = null, CancellationToken ct = default);
    public abstract Task<Stream> PersistWorkspaceAsync(CancellationToken ct = default);   // pack workspace
    public abstract Task HydrateWorkspaceAsync(Stream archive, CancellationToken ct = default); // restore workspace
    public abstract Task StopAsync(CancellationToken ct = default);
    public abstract ValueTask DisposeAsync();

    // Base class template method: starts by WorkspaceSpec + SandboxState (4 branches)
    public Task StartAsync(WorkspaceSpec spec, SandboxState? state = null, CancellationToken ct = default);
}
```

`ExecResult(ExitCode, StdOut, StdErr, Truncated)` is a `readonly record struct`.

## Startup Branches

`StartAsync(spec, state)` follows four paths depending on input:

| Branch | Condition | Behavior |
|------|------|------|
| A | spec has ephemeral entries, no state | `ApplyEphemeralEntriesAsync` applies only Ephemeral entries |
| B | Has `SandboxState` (restore scenario) | `RestoreSnapshotAsync` restores from state |
| C | State carries `SnapshotRef` | `HydrateFromSnapshotAsync` + `ApplyAllEntriesAsync` |
| D | Fresh sandbox | `InitializeFromSpecAsync` full initialization |

`WorkspaceSpecApplier.ApplyAsync(spec, targetDir, onlyEphemeral?)` handles the actual file writes for entries.

## WorkspaceSpec and Entries

```csharp
public sealed record WorkspaceSpec(
    string Root = "/workspace",
    IReadOnlyList<WorkspaceEntry>? Entries = null)
{
    public IReadOnlyDictionary<string, string> Environment { get; init; }
}
```

Entry types (abstract base: `WorkspaceEntry(Path, Ephemeral)`):

| Entry | Fields | Description |
|------|------|------|
| `FileEntry` | `Content` | Write a fixed-content file |
| `DirEntry` | — | Create a directory |
| `BindMountEntry` (Layout) | `Source`, `Target`, `ReadOnly` | Bind mount |
| `LocalDirEntry` / `LocalFileEntry` (Layout) | `HostPath`, `ContainerPath` | Local directory/file mapping |
| `GitRepoEntry` (Layout) | `RepoUrl`, `Branch="main"`, `TargetPath?` | Clone a Git repository |
| `WorkspaceProjectionEntry` (Layout) | `SourceWorkspace`, `TargetPath` | Project another workspace |

Entries with `Ephemeral = true` are rebuilt each startup and are not included in snapshots.

## State and Leases

- `SandboxState(Id, SessionId, Spec, SnapshotRef, CreatedAt)`: serializable sandbox state;
- `SandboxIsolationKey.Resolve(IsolationScope scope, RuntimeContext? ctx)`: generates isolation keys by Session / User dimensions;
- `SandboxLease(leaseId, sandboxId, ttl, onExpire?)`: lease (`Renew` / `Dispose` / `IsExpired`);
- `SandboxManager(factory?, stateStore?, agentId?)`: `AcquireAsync(ctx)` acquires sandbox with four-level priority, `ReleaseAsync` releases, `PersistStateAsync` / `ClearStateAsync` manage state;
- `SessionSandboxStateStore`: persists sandbox state with sessions.

## DockerFilesystemSpec

`DockerFilesystemSpec(ContainerWorkspace = "/workspace", HostMountSource?, ReadOnly = false, UserId?)` describes Docker container mounts (`MountTarget` property is the container workspace path).

## Snapshots

`ISandboxSnapshot` (`Id` / `Type` / `IsPersistenceEnabled`): `PersistAsync(Stream)` / `RestoreAsync()` / `IsRestorable()`. Implementations include `LocalSandboxSnapshot`, `NoopSandboxSnapshot`, `RemoteSandboxSnapshot` (via `RemoteSnapshotClient`), constructed by `LocalSnapshotSpec` / `NoopSnapshotSpec` / `RemoteSnapshotSpec` factories.

## Middleware

`SandboxLifecycleMiddleware(SandboxManager? manager = null)` (Order 50, auto-assembled) injects/removes `ctx.Items["sandbox"]` context during turns.

## External Sandbox Extensions

The following extension projects implement the `AgentScope.Extensions.Sandbox.ISandbox` interface (parallel to Harness `SandboxBase`, adapted for integration):

| Extension | Constructor |
|------|------|
| `AgentScope.Extensions.Sandbox.Docker.DockerSandbox` | `(string image = "ubuntu:22.04", string? containerName = null)` |
| `AgentScope.Extensions.Sandbox.E2B.E2BSandbox` | `(HttpClient, string apiKey, string? baseUrl = null)` |
| `AgentScope.Extensions.Sandbox.Daytona.DaytonaSandbox` | `(HttpClient, string baseUrl)` |
| `AgentScope.Extensions.Sandbox.AgentRun.AgentRunSandbox` | `(HttpClient, string baseUrl)` |
| `AgentScope.Extensions.Sandbox.Kubernetes.KubernetesSandbox` | `(string image = "ubuntu:22.04", string? kubeConfigPath = null, string? ns = null)` |

## Related Documentation

- [Filesystem](./filesystem.md) —— `SandboxBackedFilesystem`
- [Subagent](./subagent.md)
