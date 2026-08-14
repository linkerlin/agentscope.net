// Copyright 2024-2026 the original author or authors.
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
/// 项目感知覆层文件系统：在底层文件系统之上叠加“项目根”视角，
/// 自动把相对路径锚定到项目根目录，限制访问越界。
/// 对应 Java: io.agentscope.harness.agent.filesystem.ProjectAwareOverlay
/// </summary>
public sealed class ProjectAwareOverlay : IFilesystem
{
    private readonly IFilesystem _inner;
    private readonly string _projectRoot;

    public ProjectAwareOverlay(IFilesystem inner, string projectRoot)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _projectRoot = FilesystemUtils.Normalize(projectRoot);
    }

    public string ProjectRoot => _projectRoot;

    private string Anchor(string path) =>
        Path.IsPathRooted(path) ? path : FilesystemUtils.Resolve(_projectRoot, path);

    public Task<ReadResult> ReadAsync(string filePath, int? offset = null, int? limit = null, CancellationToken ct = default)
        => _inner.ReadAsync(Anchor(filePath), offset, limit, ct);

    public Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default)
        => _inner.WriteAsync(Anchor(filePath), content, ct);

    public Task<EditResult> EditAsync(string filePath, string oldString, string newString, bool replaceAll = false, CancellationToken ct = default)
        => _inner.EditAsync(Anchor(filePath), oldString, newString, replaceAll, ct);

    public Task<LsResult> ListAsync(string path, CancellationToken ct = default)
        => _inner.ListAsync(Anchor(path), ct);

    public Task<GlobResult> GlobAsync(string pattern, string? path = null, CancellationToken ct = default)
        => _inner.GlobAsync(pattern, path != null ? Anchor(path) : _projectRoot, ct);

    public Task<GrepResult> GrepAsync(string pattern, string? path = null, string? glob = null, CancellationToken ct = default)
        => _inner.GrepAsync(pattern, path != null ? Anchor(path) : _projectRoot, glob, ct);

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
        => _inner.ExistsAsync(Anchor(path), ct);

    public Task DeleteAsync(string path, CancellationToken ct = default)
        => _inner.DeleteAsync(Anchor(path), ct);

    public Task MoveAsync(string from, string to, CancellationToken ct = default)
        => _inner.MoveAsync(Anchor(from), Anchor(to), ct);
}
