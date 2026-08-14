namespace AgentScope.Harness.Sandbox.Snapshot;

/// <summary>远程快照工厂</summary>
public sealed class RemoteSnapshotSpec : ISandboxSnapshotSpec
{
    private readonly IRemoteSnapshotClient _client;

    public RemoteSnapshotSpec(IRemoteSnapshotClient client)
    {
        _client = client;
    }

    public ISandboxSnapshot Build(string snapshotId)
        => new RemoteSandboxSnapshot(_client, snapshotId);
}
