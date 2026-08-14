namespace AgentScope.Harness.Sandbox.Snapshot;

/// <summary>本地快照工厂</summary>
public sealed class LocalSnapshotSpec : ISandboxSnapshotSpec
{
    private readonly string _basePath;

    public LocalSnapshotSpec(string basePath)
    {
        _basePath = basePath;
    }

    public ISandboxSnapshot Build(string snapshotId)
        => new LocalSandboxSnapshot(_basePath, snapshotId);
}
