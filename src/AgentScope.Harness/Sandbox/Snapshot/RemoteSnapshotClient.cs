namespace AgentScope.Harness.Sandbox.Snapshot;

/// <summary>远程存储客户端接口（S3、OSS、GCS 等由用户实现）</summary>
public interface IRemoteSnapshotClient
{
    Task UploadAsync(string snapshotId, Stream data, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string snapshotId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string snapshotId, CancellationToken ct = default);
}
