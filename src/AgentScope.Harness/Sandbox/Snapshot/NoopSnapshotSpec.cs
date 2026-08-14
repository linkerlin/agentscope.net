namespace AgentScope.Harness.Sandbox.Snapshot;

/// <summary>空操作快照工厂</summary>
public sealed class NoopSnapshotSpec : ISandboxSnapshotSpec
{
    public static readonly NoopSnapshotSpec Instance = new();

    public ISandboxSnapshot Build(string snapshotId)
        => new NoopSandboxSnapshot();
}
