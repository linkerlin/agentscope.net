namespace AgentScope.Harness.Filesystem;

/// <summary>
/// 组合文件系统。按最长路径前缀路由到不同后端。对标 Java CompositeFilesystem。
/// </summary>
public sealed class CompositeFilesystem(IFilesystem defaultBackend,
    IReadOnlyDictionary<string, IFilesystem>? routes = null) : IFilesystem
{
    private readonly Dictionary<string, IFilesystem> _routes = routes?.ToDictionary(
        kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase) ?? [];

    public Task<ReadResult> ReadAsync(string filePath, int? offset = null, int? limit = null,
        CancellationToken ct = default) =>
        ResolveBackend(filePath).ReadAsync(filePath, offset, limit, ct);

    public Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default) =>
        ResolveBackend(filePath).WriteAsync(filePath, content, ct);

    public Task<EditResult> EditAsync(string filePath, string oldString, string newString,
        bool replaceAll = false, CancellationToken ct = default) =>
        ResolveBackend(filePath).EditAsync(filePath, oldString, newString, replaceAll, ct);

    public Task<LsResult> ListAsync(string path, CancellationToken ct = default) =>
        ResolveBackend(path).ListAsync(path, ct);

    public Task<GlobResult> GlobAsync(string pattern, string? path = null, CancellationToken ct = default) =>
        ResolveBackend(path ?? "").GlobAsync(pattern, path, ct);

    public Task<GrepResult> GrepAsync(string pattern, string? path = null, string? glob = null,
        CancellationToken ct = default) =>
        ResolveBackend(path ?? "").GrepAsync(pattern, path, glob, ct);

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default) =>
        ResolveBackend(path).ExistsAsync(path, ct);

    public Task DeleteAsync(string path, CancellationToken ct = default) =>
        ResolveBackend(path).DeleteAsync(path, ct);

    public Task MoveAsync(string from, string to, CancellationToken ct = default) =>
        ResolveBackend(from).MoveAsync(from, to, ct);

    private IFilesystem ResolveBackend(string path)
    {
        // 按最长前缀匹配
        var best = "";
        IFilesystem? backend = null;
        foreach (var (prefix, fs) in _routes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && prefix.Length > best.Length)
            {
                best = prefix;
                backend = fs;
            }
        }
        return backend ?? defaultBackend;
    }
}
