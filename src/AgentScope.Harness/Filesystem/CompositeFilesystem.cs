// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace AgentScope.Harness.Filesystem;

/// <summary>
/// Composite filesystem. Routes to different backends by longest path prefix matching.
/// 组合文件系统。按最长路径前缀路由到不同后端。
/// Counterpart to Java CompositeFilesystem.
/// </summary>
public sealed class CompositeFilesystem(IFilesystem defaultBackend,
    IReadOnlyDictionary<string, IFilesystem>? routes = null) : IFilesystem
{
    private readonly Dictionary<string, IFilesystem> _routes = routes?.ToDictionary(
        kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase) ?? [];

    /// <inheritdoc />
    public Task<ReadResult> ReadAsync(string filePath, int? offset = null, int? limit = null,
        CancellationToken ct = default) =>
        ResolveBackend(filePath).ReadAsync(filePath, offset, limit, ct);

    /// <inheritdoc />
    public Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default) =>
        ResolveBackend(filePath).WriteAsync(filePath, content, ct);

    /// <inheritdoc />
    public Task<EditResult> EditAsync(string filePath, string oldString, string newString,
        bool replaceAll = false, CancellationToken ct = default) =>
        ResolveBackend(filePath).EditAsync(filePath, oldString, newString, replaceAll, ct);

    /// <inheritdoc />
    public Task<LsResult> ListAsync(string path, CancellationToken ct = default) =>
        ResolveBackend(path).ListAsync(path, ct);

    /// <inheritdoc />
    public Task<GlobResult> GlobAsync(string pattern, string? path = null, CancellationToken ct = default) =>
        ResolveBackend(path ?? "").GlobAsync(pattern, path, ct);

    /// <inheritdoc />
    public Task<GrepResult> GrepAsync(string pattern, string? path = null, string? glob = null,
        CancellationToken ct = default) =>
        ResolveBackend(path ?? "").GrepAsync(pattern, path, glob, ct);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string path, CancellationToken ct = default) =>
        ResolveBackend(path).ExistsAsync(path, ct);

    /// <inheritdoc />
    public Task DeleteAsync(string path, CancellationToken ct = default) =>
        ResolveBackend(path).DeleteAsync(path, ct);

    /// <inheritdoc />
    public Task MoveAsync(string from, string to, CancellationToken ct = default) =>
        ResolveBackend(from).MoveAsync(from, to, ct);

    /// <summary>
    /// Resolve the matching backend for the given path using longest prefix matching.
    /// 根据最长前缀匹配为给定路径解析对应的后端文件系统。
    /// </summary>
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
