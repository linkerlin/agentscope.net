namespace AgentScope.Extensions.Sandbox;

/// <summary>
/// 沙箱接口。对标 Java Sandbox/AbstractBaseSandbox。
/// </summary>
public interface ISandbox : IAsyncDisposable
{
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);
    Task<ExecResult> ExecAsync(string command, int? timeoutSeconds = null, CancellationToken ct = default);
    Task<Stream> PersistWorkspaceAsync(CancellationToken ct = default);
    Task HydrateWorkspaceAsync(Stream archive, CancellationToken ct = default);
}

/// <summary>
/// 沙箱客户端工厂接口。对标 Java SandboxClient。
/// 除创建/恢复外，还负责删除、状态序列化与反序列化（用于持久化会话）。
/// </summary>
public interface ISandboxClient
{
    Task<ISandbox> CreateAsync(WorkspaceSpec spec, CancellationToken ct = default);
    Task<ISandbox> ResumeAsync(SandboxState state, CancellationToken ct = default);
    Task DeleteAsync(ISandbox sandbox, CancellationToken ct = default);
    string SerializeState(SandboxState state);
    SandboxState DeserializeState(string json);
}

public readonly record struct ExecResult(int ExitCode, string StdOut, string StdErr, bool Truncated);

public sealed record WorkspaceSpec(string Root = "/workspace",
    IReadOnlyList<WorkspaceEntry>? Entries = null,
    IReadOnlyDictionary<string, string>? Environment = null);

public abstract record WorkspaceEntry(string Path, bool Ephemeral = false);
public sealed record FileEntry(string Path, string Content, bool Ephemeral = false) : WorkspaceEntry(Path, Ephemeral);
public sealed record DirEntry(string Path, bool Ephemeral = false) : WorkspaceEntry(Path, Ephemeral);

public sealed record SandboxState(string Id, WorkspaceSpec Spec, string SnapshotRef)
{
    /// <summary>会话标识（部分 provider 用它做沙箱命名/恢复）。</summary>
    public string? SessionId { get; init; }

    /// <summary>工作区根路径（provider 可覆盖，默认取 Spec.Root）。</summary>
    public string? WorkspaceRoot { get; init; }

    /// <summary>provider 特有附加状态，序列化时一并保存。</summary>
    public Dictionary<string, object?>? ProviderData { get; init; }
}
