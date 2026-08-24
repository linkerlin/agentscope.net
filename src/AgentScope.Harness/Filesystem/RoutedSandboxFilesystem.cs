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
/// 路由型沙箱文件系统：按路径前缀把文件操作路由到不同的底层文件系统（如本地 + 多沙箱）。
/// 对应 Java: io.agentscope.harness.agent.filesystem.RoutedSandboxFilesystem
/// </summary>
public sealed class RoutedSandboxFilesystem : IFilesystem
{
    private readonly List<Route> _routes;
    private readonly IFilesystem _fallback;

    /// <param name="fallback">未命中任何路由时使用的默认文件系统。</param>
    /// <param name="routes">路由规则（前缀 -> 文件系统）。</param>
    public RoutedSandboxFilesystem(IFilesystem fallback, IEnumerable<Route>? routes = null)
    {
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _routes = (routes ?? Array.Empty<Route>()).OrderByDescending(r => r.Prefix.Length).ToList();
    }

    private IFilesystem RouteFor(string path)
    {
        var n = FilesystemUtils.Normalize(path);
        foreach (var route in _routes)
        {
            if (n.StartsWith(route.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return route.Target;
            }
        }

        return _fallback;
    }

    public Task<ReadResult> ReadAsync(string filePath, int? offset = null, int? limit = null, CancellationToken ct = default)
        => RouteFor(filePath).ReadAsync(filePath, offset, limit, ct);
    public Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default)
        => RouteFor(filePath).WriteAsync(filePath, content, ct);
    public Task<EditResult> EditAsync(string filePath, string oldString, string newString, bool replaceAll = false, CancellationToken ct = default)
        => RouteFor(filePath).EditAsync(filePath, oldString, newString, replaceAll, ct);
    public Task<LsResult> ListAsync(string path, CancellationToken ct = default)
        => RouteFor(path).ListAsync(path, ct);
    public Task<GlobResult> GlobAsync(string pattern, string? path = null, CancellationToken ct = default)
        => RouteFor(path ?? "/").GlobAsync(pattern, path, ct);
    public Task<GrepResult> GrepAsync(string pattern, string? path = null, string? glob = null, CancellationToken ct = default)
        => RouteFor(path ?? "/").GrepAsync(pattern, path, glob, ct);
    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
        => RouteFor(path).ExistsAsync(path, ct);
    public Task DeleteAsync(string path, CancellationToken ct = default)
        => RouteFor(path).DeleteAsync(path, ct);
    public Task MoveAsync(string from, string to, CancellationToken ct = default)
        => RouteFor(from).MoveAsync(from, to, ct);

    /// <summary>路由规则。</summary>
    public sealed record Route(string Prefix, IFilesystem Target);
}
