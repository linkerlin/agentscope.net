using AgentScope.Core.Agent;

namespace AgentScope.Harness.Sandbox;

/// <summary>
/// 沙箱上下文。对标 Java SandboxContext。
/// </summary>
public sealed record SandboxContext(
    IsolationScope IsolationScope,
    WorkspaceSpec WorkspaceSpec,
    object? ExternalSandbox = null,
    object? ExternalSandboxState = null)
{
    public static readonly SandboxContext Default = new(IsolationScope.Session, new WorkspaceSpec());
}

/// <summary>
/// Harness 工作区规格。
/// </summary>
public sealed record WorkspaceSpec(
    string Root = "/workspace",
    IReadOnlyList<WorkspaceEntry>? Entries = null)
{
    public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// 工作区条目基类。
/// </summary>
public abstract record WorkspaceEntry(string Path, bool Ephemeral = false);
public sealed record FileEntry(string Path, string Content, bool Ephemeral = false) : WorkspaceEntry(Path, Ephemeral);
public sealed record DirEntry(string Path, bool Ephemeral = false) : WorkspaceEntry(Path, Ephemeral);

/// <summary>
/// 沙箱管理器。对标 Java SandboxManager。
/// Acquire 优先级：外部沙箱 &gt; 外部状态 &gt; 持久化状态 &gt; 新建。
/// </summary>
public sealed class SandboxManager
{
    private readonly SessionSandboxStateStore? _stateStore;
    private readonly string _agentId;
    private readonly SandboxExecutionGuard? _executionGuard;
    private readonly Func<SandboxContext, CancellationToken, Task<SandboxBase?>>? _factory;

    public SandboxManager(
        Func<SandboxContext, CancellationToken, Task<SandboxBase?>>? factory = null,
        SessionSandboxStateStore? stateStore = null,
        string agentId = "",
        SandboxExecutionGuard? executionGuard = null)
    {
        _factory = factory;
        _stateStore = stateStore;
        _agentId = agentId;
        _executionGuard = executionGuard;
    }

    /// <summary>
    /// 按 4 级优先级获取沙箱。对标 Java SandboxManager.acquire。
    /// </summary>
    public async Task<SandboxAcquireResult> AcquireAsync(
        SandboxContext ctx, RuntimeContext? runtimeContext = null, CancellationToken ct = default)
    {
        // 优先级 1：用户提供的外部沙箱（guard 不适用）
        if (ctx.ExternalSandbox is SandboxBase external)
            return new SandboxAcquireResult(external, false);

        // 优先级 2：用户提供的外部状态（guard 不适用）
        if (ctx.ExternalSandboxState is SandboxState extState)
        {
            var sandbox = await CreateOrResumeAsync(ctx, extState, ct).ConfigureAwait(false);
            if (sandbox != null) return new SandboxAcquireResult(sandbox, true);
        }

        // 优先级 3 / 4：harness 自管理
        if (_stateStore != null)
        {
            var scopeKey = SandboxIsolationKey.Resolve(ctx.IsolationScope, runtimeContext);
            var state = await _stateStore.LoadAsync(scopeKey.SessionId ?? "", ct).ConfigureAwait(false);
            if (state != null)
            {
                var sandbox = await CreateOrResumeAsync(ctx, state, ct).ConfigureAwait(false);
                if (sandbox != null) return new SandboxAcquireResult(sandbox, true);
            }
        }

        // 优先级 4：新建
        var fresh = await CreateOrResumeAsync(ctx, null, ct).ConfigureAwait(false);
        return new SandboxAcquireResult(fresh, fresh != null);
    }

    /// <summary>
    /// 释放沙箱：对 self-managed 沙箱执行 stop + dispose（shutdown）。
    /// 对标 Java SandboxManager.release。
    /// </summary>
    public async Task ReleaseAsync(SandboxAcquireResult result)
    {
        if (result.Sandbox is not SandboxBase sb || !result.IsSelfManaged)
            return;
        try { await sb.StopAsync().ConfigureAwait(false); }
        catch { /* 记录失败但继续 shutdown */ }
        try { await sb.DisposeAsync().ConfigureAwait(false); }
        catch { }
    }

    /// <summary>
    /// 持久化沙箱状态。对标 Java SandboxManager.persistState。
    /// </summary>
    public async Task PersistStateAsync(
        SandboxAcquireResult result, SandboxContext? ctx, RuntimeContext? runtimeContext)
    {
        if (_stateStore == null || result.Sandbox is not SandboxBase || !result.IsSelfManaged)
            return;
        if (ctx == null) return;

        var scopeKey = SandboxIsolationKey.Resolve(ctx.IsolationScope, runtimeContext);
        if (string.IsNullOrEmpty(scopeKey.SessionId)) return;

        // 从工作区重新构建状态（以当前 spec 与 session 为准）
        var state = new SandboxState(
            Id: _agentId,
            SessionId: scopeKey.SessionId,
            Spec: ctx.WorkspaceSpec,
            SnapshotRef: "");
        await _stateStore.SaveAsync(scopeKey.SessionId, state).ConfigureAwait(false);
    }

    /// <summary>
    /// 清理沙箱状态。对标 Java SandboxManager.clearState。
    /// </summary>
    public async Task ClearStateAsync(SandboxContext? ctx, RuntimeContext? runtimeContext)
    {
        if (_stateStore == null || ctx == null) return;
        var scopeKey = SandboxIsolationKey.Resolve(ctx.IsolationScope, runtimeContext);
        if (string.IsNullOrEmpty(scopeKey.SessionId)) return;
        await _stateStore.DeleteAsync(scopeKey.SessionId).ConfigureAwait(false);
    }

    private async Task<SandboxBase?> CreateOrResumeAsync(
        SandboxContext ctx, SandboxState? state, CancellationToken ct)
    {
        if (_factory == null) return null;
        var sandbox = await _factory(ctx, ct).ConfigureAwait(false);
        if (sandbox == null) return null;
        await sandbox.StartAsync(state?.Spec ?? ctx.WorkspaceSpec, state, ct).ConfigureAwait(false);
        return sandbox;
    }
}

public sealed record SandboxAcquireResult(object? Sandbox, bool IsSelfManaged);
