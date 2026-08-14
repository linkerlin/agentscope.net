using AgentScope.Core.Agent;

namespace AgentScope.Harness.Sandbox;

public readonly record struct ExecResult(int ExitCode, string StdOut, string StdErr, bool Truncated);

/// <summary>
/// 沙箱抽象基类，实现 4 分支启动逻辑。对标 Java AbstractBaseSandbox。
/// </summary>
public abstract class SandboxBase : IAsyncDisposable
{
    private bool _workspaceRootReady;
    private bool _canRestoreSnapshot;
    private WorkspaceSpec? _spec;
    private readonly WorkspaceSpecApplier _specApplier = new();

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

    public abstract Task<ExecResult> ExecAsync(string command, int? timeoutSeconds = null, CancellationToken ct = default);
    public abstract Task<Stream> PersistWorkspaceAsync(CancellationToken ct = default);
    public abstract Task HydrateWorkspaceAsync(Stream archive, CancellationToken ct = default);
    public abstract Task StopAsync(CancellationToken ct = default);
    public abstract ValueTask DisposeAsync();

    protected abstract string WorkspaceRoot { get; }

    /// <summary>Branch A/B：仅应用 ephemeral 条目。对标 Java applyWorkspaceSpec(spec, onlyEphemeral=true)。</summary>
    protected virtual Task ApplyEphemeralEntriesAsync(CancellationToken ct)
        => _spec == null ? Task.CompletedTask : _specApplier.ApplyAsync(_spec, WorkspaceRoot, onlyEphemeral: true, ct);

    /// <summary>Branch B：从快照恢复工作区。快照流由具体后端子类提供（C# 侧快照体系以 SnapshotRef 表示），默认无操作。</summary>
    protected virtual Task RestoreSnapshotAsync(SandboxState state, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Branch C：从快照引用水合工作区。默认无操作，由后端子类实现真实恢复。</summary>
    protected virtual Task HydrateFromSnapshotAsync(string snapshotRef, CancellationToken ct) => Task.CompletedTask;

    /// <summary>Branch C：应用全部条目。对标 Java applyWorkspaceSpec(spec, onlyEphemeral=false)。</summary>
    protected virtual Task ApplyAllEntriesAsync(WorkspaceSpec spec, CancellationToken ct)
        => _specApplier.ApplyAsync(spec, WorkspaceRoot, onlyEphemeral: false, ct);

    /// <summary>Branch D：全新初始化工作区（确保根目录存在并应用全部条目）。</summary>
    protected virtual Task InitializeFromSpecAsync(WorkspaceSpec spec, CancellationToken ct)
    {
        Directory.CreateDirectory(WorkspaceRoot);
        return _specApplier.ApplyAsync(spec, WorkspaceRoot, onlyEphemeral: false, ct);
    }
}

/// <summary>
/// 可序列化的沙箱状态。对标 Java SandboxState。
/// </summary>
public sealed record SandboxState(
    string Id,
    string SessionId,
    WorkspaceSpec Spec,
    string SnapshotRef,
    DateTime CreatedAt = default)
{
    public DateTime CreatedAt { get; init; } = CreatedAt == default ? DateTime.UtcNow : CreatedAt;
}

/// <summary>
/// 沙箱隔离键。对标 Java SandboxIsolationKey。
/// </summary>
public sealed record SandboxIsolationKey(IsolationScope Scope, string? SessionId, string? UserId = null)
{
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
/// </summary>
public sealed class WorkspaceSpecApplier
{
    /// <summary>
    /// 将工作区规格落地到目标目录。对标 Java applyWorkspaceSpec。
    /// </summary>
    /// <param name="spec">工作区规格</param>
    /// <param name="targetDir">目标目录</param>
    /// <param name="onlyEphemeral">仅应用 ephemeral 条目（Branch A/B），否则应用全部（Branch C/D）</param>
    /// <param name="ct">取消令牌</param>
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
/// </summary>
public sealed class SandboxExecutionGuard
{
    private readonly SemaphoreSlim _sem = new(1, 1);

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        await _sem.WaitAsync(ct);
        try { return await action(); }
        finally { _sem.Release(); }
    }
}
