namespace AgentScope.Harness.Sandbox.Snapshot;

/// <summary>空操作快照：不执行任何持久化</summary>
public sealed class NoopSandboxSnapshot : ISandboxSnapshot
{
    public string Id => "noop";
    public string Type => "noop";
    public bool IsPersistenceEnabled => false;

    public Task PersistAsync(Stream data, CancellationToken ct = default)
    {
        data.Dispose();
        return Task.CompletedTask;
    }

    public Task<Stream> RestoreAsync(CancellationToken ct = default)
        => throw new NotSupportedException("Noop snapshot cannot be restored");

    public bool IsRestorable() => false;
}
