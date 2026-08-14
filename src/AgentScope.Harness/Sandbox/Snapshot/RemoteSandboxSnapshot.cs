namespace AgentScope.Harness.Sandbox.Snapshot;

/// <summary>远程存储快照实现：委派给 IRemoteSnapshotClient</summary>
public sealed class RemoteSandboxSnapshot : ISandboxSnapshot
{
    private readonly IRemoteSnapshotClient? _client;

    public RemoteSandboxSnapshot(IRemoteSnapshotClient client, string snapshotId)
    {
        _client = client;
        Id = snapshotId;
    }

    public string Id { get; }
    public string Type => "remote";
    public bool IsPersistenceEnabled => true;

    public async Task PersistAsync(Stream data, CancellationToken ct = default)
    {
        var client = RequireClient();
        await client.UploadAsync(Id, data, ct);
    }

    public async Task<Stream> RestoreAsync(CancellationToken ct = default)
    {
        var client = RequireClient();
        return await client.DownloadAsync(Id, ct);
    }

    public async Task<bool> IsRestorableAsync(CancellationToken ct = default)
    {
        var client = RequireClient();
        return await client.ExistsAsync(Id, ct);
    }

    public bool IsRestorable() => true;

    private IRemoteSnapshotClient RequireClient()
        => _client ?? throw new InvalidOperationException(
            "RemoteSnapshotClient is required for remote snapshot operations");
}
