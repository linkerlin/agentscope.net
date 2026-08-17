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

using System.Collections.Concurrent;

namespace AgentScope.Harness.Workspace;

/// <summary>
/// 工作区管理器。对标 Java WorkspaceManager。
/// 提供工作区文件的双层读取架构（IFilesystem 优先 → 本地磁盘回退）。
/// </summary>
public sealed class WorkspaceManager(
    string workspaceRoot,
    bool sandboxed = true) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private readonly PathPolicy _policy = PathPolicy.FromWorkspace(workspaceRoot);
    private readonly LocalFsMode _mode = sandboxed ? LocalFsMode.Sandboxed : LocalFsMode.Unrestricted;

    public string WorkspaceRoot => workspaceRoot;

    public async Task<string?> ReadAsync(string relativePath, CancellationToken ct = default)
    {
        var full = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));
        _policy.EnsureAllowed(full);

        if (_cache.TryGetValue(relativePath, out var cached))
            return cached;

        if (File.Exists(full))
        {
            var content = await File.ReadAllTextAsync(full, ct);
            _cache[relativePath] = content;
            return content;
        }

        return null;
    }

    public async Task WriteAsync(string relativePath, string content, CancellationToken ct = default)
    {
        var full = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));
        _policy.EnsureAllowed(full);
        var dir = Path.GetDirectoryName(full);
        if (dir != null) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(full, content, ct);
        _cache[relativePath] = content;
    }

    public bool Exists(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));
        return File.Exists(full) || Directory.Exists(full);
    }

    // ── 语义读取 API：对标 Java WorkspaceManager 的 readAgentsMd / readMemoryMd 等 ──

    /// <summary>读取 AGENTS.md（Agent 行为约定）。对标 Java <c>readAgentsMd</c>。</summary>
    public Task<string?> ReadAgentsMdAsync(CancellationToken ct = default)
        => ReadAsync(WorkspaceConstants.AgentsMd, ct);

    /// <summary>读取 MEMORY.md（长期记忆）。对标 Java <c>readMemoryMd</c>。</summary>
    public Task<string?> ReadMemoryMdAsync(CancellationToken ct = default)
        => ReadAsync(WorkspaceConstants.MemoryMd, ct);

    /// <summary>读取 KNOWLEDGE.md（领域知识索引）。对标 Java <c>readKnowledgeMd</c>。</summary>
    public Task<string?> ReadKnowledgeMdAsync(CancellationToken ct = default)
        => ReadAsync(WorkspaceConstants.KnowledgeMd, ct);

    /// <summary>
    /// 列出 knowledge/ 目录下的知识文件（相对工作区根的路径）。
    /// 对标 Java <c>listKnowledgeFiles</c>。
    /// </summary>
    public IReadOnlyList<string> ListKnowledgeFiles()
        => ListFiles(WorkspaceConstants.KnowledgeDir, "*.*");

    /// <summary>
    /// 按 glob 模式列出某个子目录下的文件，返回相对工作区根的路径（正斜杠分隔）。
    /// 对标 Java <c>AbstractFilesystem.glob</c> 在 WorkspaceManager 上的常用组合。
    /// </summary>
    public IReadOnlyList<string> ListFiles(string relativeDir, string pattern = "*")
    {
        var dir = Path.GetFullPath(Path.Combine(workspaceRoot, relativeDir));
        _policy.EnsureAllowed(dir);
        if (!Directory.Exists(dir)) return [];

        return Directory
            .EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly)
            .Select(ToRelative)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>获取文件最后写入时间；文件不存在返回 null。</summary>
    public DateTime? GetLastWriteTimeUtc(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));
        _policy.EnsureAllowed(full);
        return File.Exists(full) ? File.GetLastWriteTimeUtc(full) : null;
    }

    /// <summary>移动/重命名工作区内文件（自动建目标目录）。对标 Java <c>fs.move</c>。</summary>
    public void Move(string fromRelative, string toRelative)
    {
        var from = Path.GetFullPath(Path.Combine(workspaceRoot, fromRelative));
        var to = Path.GetFullPath(Path.Combine(workspaceRoot, toRelative));
        _policy.EnsureAllowed(from);
        _policy.EnsureAllowed(to);
        if (!File.Exists(from)) return;

        var dir = Path.GetDirectoryName(to);
        if (dir != null) Directory.CreateDirectory(dir);
        File.Move(from, to, overwrite: true);
        _cache.TryRemove(fromRelative, out _);
    }

    /// <summary>删除工作区内文件。对标 Java <c>fs.delete</c>。</summary>
    public void Delete(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));
        _policy.EnsureAllowed(full);
        if (File.Exists(full)) File.Delete(full);
        _cache.TryRemove(relativePath, out _);
    }

    private string ToRelative(string fullPath)
        => Path.GetRelativePath(workspaceRoot, fullPath).Replace('\\', '/');

    public ValueTask DisposeAsync()
    {
        _cache.Clear();
        return ValueTask.CompletedTask;
    }
}
