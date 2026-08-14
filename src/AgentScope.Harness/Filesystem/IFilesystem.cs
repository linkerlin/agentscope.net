namespace AgentScope.Harness.Filesystem;

/// <summary>
/// 核心文件系统接口。对标 Java AbstractFilesystem。
/// RuntimeContext 通过 AsyncLocal 隐式传递，不在方法签名中显式传递。
/// </summary>
public interface IFilesystem
{
    Task<ReadResult> ReadAsync(string filePath, int? offset = null, int? limit = null, CancellationToken ct = default);
    Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default);
    Task<EditResult> EditAsync(string filePath, string oldString, string newString, bool replaceAll = false, CancellationToken ct = default);
    Task<LsResult> ListAsync(string path, CancellationToken ct = default);
    Task<GlobResult> GlobAsync(string pattern, string? path = null, CancellationToken ct = default);
    Task<GrepResult> GrepAsync(string pattern, string? path = null, string? glob = null, CancellationToken ct = default);
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    Task MoveAsync(string from, string to, CancellationToken ct = default);
}

// ── DTO records ──

public readonly record struct ReadResult(string? Content, bool Found, string? Error = null);
public readonly record struct WriteResult(bool Success, string? Error = null);
public readonly record struct EditResult(bool Success, string? Error = null);

public readonly record struct LsResult(IReadOnlyList<FileInfo> Files, string? Error = null);
public readonly record struct FileInfo(string Name, string Path, bool IsDirectory, long Size, DateTime LastModified);

public readonly record struct GlobResult(IReadOnlyList<string> Paths, string? Error = null);
public readonly record struct GrepResult(IReadOnlyList<GrepMatch> Matches, string? Error = null);
public readonly record struct GrepMatch(string File, int LineNumber, string Line);
