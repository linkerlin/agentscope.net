// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using AgentScope.Core.Agent;

namespace AgentScope.Harness.Sandbox;

/// <summary>
/// 命令执行结果。包含退出码、标准输出/错误及截断标记。
/// Command execution result containing exit code, stdout/stderr, and truncation flag.
/// </summary>
public readonly record struct ExecResult(int ExitCode, string StdOut, string StdErr, bool Truncated);

/// <summary>
/// 沙箱抽象基类，实现 4 分支启动逻辑。对标 Java AbstractBaseSandbox。
/// Abstract sandbox base class implementing 4-branch startup logic. Counterpart to Java AbstractBaseSandbox.
/// </summary>
public abstract class SandboxBase : IAsyncDisposable
{
    private bool _workspaceRootReady;
    private bool _canRestoreSnapshot;
    private WorkspaceSpec? _spec;
    private readonly WorkspaceSpecApplier _specApplier = new();

    /// <summary>
    /// 异步启动沙箱，根据工作区根目录状态选择启动分支（A/B/C/D）。
    /// Start the sandbox asynchronously, selecting startup branch (A/B/C/D) based on workspace root state.
    /// </summary>
    /// <param name="spec">工作区规格 / Workspace specification</param>
    /// <param name="state">可选的沙箱状态（含快照引用） / Optional sandbox state (with snapshot reference)</param>
    /// <param name="ct">取消令牌 / Cancellation token</param>
    public async Task StartAsync(WorkspaceSpec spec, SandboxState? state = null, CancellationToken ct = default)
    {
        _spec = spec;
        _workspaceRootReady = !string.IsNullOrEmpty(WorkspaceRoot) && Directory.Exists(WorkspaceRoot);
        _canRestoreSnapshot = state != null && !string.IsNullOrEmpty(state.SnapshotRef);

        if (_workspaceRootReady && Directory.Exists(WorkspaceRoot))
            await ApplyEphemeralEntriesAsync(ct);                                // Branch A
        else if (_workspaceRootReady && !Directory.Exists(WorkspaceRoot))
        {
            await RestoreSnapshotAsync(state!, ct);                               // Branch B
            await ApplyEphemeralEntriesAsync(ct);
        }
        else if (!_workspaceRootReady && _canRestoreSnapshot)
        {
            await HydrateFromSnapshotAsync(state!.SnapshotRef, ct);               // Branch C
            await ApplyAllEntriesAsync(spec, ct);
        }
        else
            await InitializeFromSpecAsync(spec, ct);                              // Branch D
    }

    /// <summary>
    /// 在沙箱中异步执行命令。
    /// Execute a command in the sandbox asynchronously.
    /// </summary>
    /// <param name="command">要执行的命令 / Command to execute</param>
    /// <param name="timeoutSeconds">超时秒数（可选） / Timeout in seconds (optional)</param>
    /// <param name="ct">取消令牌 / Cancellation token</param>
    public abstract Task<ExecResult> ExecAsync(string command, int? timeoutSeconds = null, CancellationToken ct = default);

    /// <summary>
    /// 持久化当前工作区为流。
    /// Persist the current workspace as a stream.
    /// </summary>
    /// <param name="ct">取消令牌 / Cancellation token</param>
    public abstract Task<Stream> PersistWorkspaceAsync(CancellationToken ct = default);

    /// <summary>
    /// 从归档流恢复工作区。
    /// Hydrate the workspace from an archive stream.
    /// </summary>
    /// <param name="archive">包含工作区数据的归档流 / Archive stream containing workspace data</param>
    /// <param name="ct">取消令牌 / Cancellation token</param>
    public abstract Task HydrateWorkspaceAsync(Stream archive, CancellationToken ct = default);

    /// <summary>
    /// 停止沙箱。
    /// Stop the sandbox.
    /// </summary>
    /// <param name="ct">取消令牌 / Cancellation token</param>
    public abstract Task StopAsync(CancellationToken ct = default);

    /// <inheritdoc />
    public abstract ValueTask DisposeAsync();

    /// <summary>
    /// 沙箱工作区根目录路径（由子类提供）。
    /// Sandbox workspace root directory path (provided by subclass).
    /// </summary>
    protected abstract string WorkspaceRoot { get; }

    /// <summary>
    /// Branch A/B：仅应用 ephemeral 条目。对标 Java applyWorkspaceSpec(spec, onlyEphemeral=true)。
    /// Branch A/B: apply only ephemeral entries.
    /// </summary>
    protected virtual Task ApplyEphemeralEntriesAsync(CancellationToken ct)
        => _spec == null ? Task.CompletedTask : _specApplier.ApplyAsync(_spec, WorkspaceRoot, onlyEphemeral: true, ct);

    /// <summary>
    /// Branch B：从快照恢复工作区。快照流由具体后端子类提供，默认无操作。
    /// Branch B: restore workspace from snapshot. Stream provided by concrete subclass. No-op by default.
    /// </summary>
    protected virtual Task RestoreSnapshotAsync(SandboxState state, CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// Branch C：从快照引用水合工作区。默认无操作，由后端子类实现真实恢复。
    /// Branch C: hydrate workspace from snapshot reference. No-op by default; subclass should implement actual restore.
    /// </summary>
    protected virtual Task HydrateFromSnapshotAsync(string snapshotRef, CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// Branch C：应用全部条目。对标 Java applyWorkspaceSpec(spec, onlyEphemeral=false)。
    /// Branch C: apply all entries (both ephemeral and persistent).
    /// </summary>
    protected virtual Task ApplyAllEntriesAsync(WorkspaceSpec spec, CancellationToken ct)
        => _specApplier.ApplyAsync(spec, WorkspaceRoot, onlyEphemeral: false, ct);

    /// <summary>
    /// Branch D：全新初始化工作区（确保根目录存在并应用全部条目）。
    /// Branch D: initialize workspace from scratch (ensure root directory exists and apply all entries).
    /// </summary>
    protected virtual Task InitializeFromSpecAsync(WorkspaceSpec spec, CancellationToken ct)
    {
        Directory.CreateDirectory(WorkspaceRoot);
        return _specApplier.ApplyAsync(spec, WorkspaceRoot, onlyEphemeral: false, ct);
    }
}

/// <summary>
/// 可序列化的沙箱状态。对标 Java SandboxState。
/// Serializable sandbox state. Counterpart to Java SandboxState.
/// </summary>
public sealed record SandboxState(
    string Id,
    string SessionId,
    WorkspaceSpec Spec,
    string SnapshotRef,
    DateTime CreatedAt = default)
{
    /// <summary>
    /// 创建时间（默认 UTC 当前时间）。
    /// Creation time (defaults to current UTC time).
    /// </summary>
    public DateTime CreatedAt { get; init; } = CreatedAt == default ? DateTime.UtcNow : CreatedAt;
}

/// <summary>
/// 沙箱隔离键。对标 Java SandboxIsolationKey。
/// Sandbox isolation key. Counterpart to Java SandboxIsolationKey.
/// </summary>
public sealed record SandboxIsolationKey(IsolationScope Scope, string? SessionId, string? UserId = null)
{
    /// <summary>
    /// 根据隔离范围与运行上下文解析隔离键。
    /// Resolve the isolation key based on the isolation scope and runtime context.
    /// </summary>
    /// <param name="scope">隔离范围 / Isolation scope</param>
    /// <param name="ctx">运行上下文 / Runtime context</param>
    /// <returns>解析后的隔离键 / Resolved isolation key</returns>
    public static SandboxIsolationKey Resolve(IsolationScope scope, RuntimeContext? ctx) => scope switch
    {
        IsolationScope.Session => new(scope, ctx?.SessionId ?? "default"),
        IsolationScope.User => new(scope, ctx?.SessionId, ctx?.UserId),
        IsolationScope.Agent => new(scope, "agent_shared"),
        _ => new(scope, "global")
    };
}

/// <summary>
/// 工作区规格应用器。对标 Java WorkspaceSpecApplier。
/// Workspace spec applier. Counterpart to Java WorkspaceSpecApplier.
/// </summary>
public sealed class WorkspaceSpecApplier
{
    /// <summary>
    /// 将工作区规格落地到目标目录。对标 Java applyWorkspaceSpec。
    /// Apply the workspace specification to the target directory.
    /// </summary>
    /// <param name="spec">工作区规格 / Workspace specification</param>
    /// <param name="targetDir">目标目录 / Target directory</param>
    /// <param name="onlyEphemeral">仅应用 ephemeral 条目（Branch A/B），否则应用全部（Branch C/D） / Apply only ephemeral entries (Branch A/B), otherwise all (Branch C/D)</param>
    /// <param name="ct">取消令牌 / Cancellation token</param>
    public async Task ApplyAsync(WorkspaceSpec spec, string targetDir, bool onlyEphemeral = false, CancellationToken ct = default)
    {
        if (spec.Entries == null) return;
        Directory.CreateDirectory(targetDir);

        foreach (var entry in spec.Entries)
        {
            ct.ThrowIfCancellationRequested();
            if (onlyEphemeral && !entry.Ephemeral) continue;

            var dest = Path.Combine(targetDir, entry.Path.TrimStart('/'));

            switch (entry)
            {
                case FileEntry fe:
                    var dir = Path.GetDirectoryName(dest);
                    if (dir != null) Directory.CreateDirectory(dir);
                    await File.WriteAllTextAsync(dest, fe.Content ?? "", ct);
                    break;
                case DirEntry:
                    Directory.CreateDirectory(dest);
                    break;
            }
        }
    }
}

/// <summary>
/// 沙箱并发执行守卫。对标 Java SandboxExecutionGuard。
/// Sandbox concurrent execution guard. Counterpart to Java SandboxExecutionGuard.
/// </summary>
public sealed class SandboxExecutionGuard
{
    private readonly SemaphoreSlim _sem = new(1, 1);

    /// <summary>
    /// 在互斥锁下异步执行操作，保证同一时刻只有一个命令在沙箱中执行。
    /// Execute an action under mutual exclusion, ensuring only one command runs in the sandbox at a time.
    /// </summary>
    /// <param name="action">要执行的操作 / Action to execute</param>
    /// <param name="ct">取消令牌 / Cancellation token</param>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        await _sem.WaitAsync(ct);
        try { return await action(); }
        finally { _sem.Release(); }
    }
}
