namespace AgentScope.Harness.Sandbox.Snapshot;

/// <summary>本地文件系统快照实现：tar 归档到本地路径</summary>
public sealed class LocalSandboxSnapshot : ISandboxSnapshot
{
    private readonly string _basePath;

    public LocalSandboxSnapshot(string basePath, string snapshotId)
    {
        if (!IsValidId(snapshotId))
            throw new ArgumentException($"Invalid snapshot ID: {snapshotId}");
        _basePath = basePath;
        Id = snapshotId;
        Directory.CreateDirectory(_basePath);
    }

    public string Id { get; }
    public string Type => "local";
    public bool IsPersistenceEnabled => true;

    private string ArchivePath => Path.Combine(_basePath, $"{Id}.tar");

    /// <summary>将工作区流写入本地 tar 文件（原子写入）</summary>
    public async Task PersistAsync(Stream data, CancellationToken ct = default)
    {
        var tmpPath = Path.Combine(_basePath, $".{Id}.{Guid.NewGuid()}.tmp");
        try
        {
            using var fileStream = File.Create(tmpPath);
            await data.CopyToAsync(fileStream, ct);
            await fileStream.FlushAsync(ct);
            File.Move(tmpPath, ArchivePath, overwrite: true);
        }
        catch
        {
            try { File.Delete(tmpPath); } catch { }
            throw;
        }
    }

    /// <summary>从本地 tar 文件读取流</summary>
    public Task<Stream> RestoreAsync(CancellationToken ct = default)
    {
        if (!File.Exists(ArchivePath))
            throw new FileNotFoundException($"Snapshot not found: {ArchivePath}");
        return Task.FromResult<Stream>(File.OpenRead(ArchivePath));
    }

    public bool IsRestorable() => File.Exists(ArchivePath);

    private static bool IsValidId(string id)
    {
        return !string.IsNullOrWhiteSpace(id)
            && !id.Contains('/')
            && !id.Contains('\\')
            && !id.Contains("..")
            && !id.Contains('\0');
    }
}
