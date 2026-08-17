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

using System.Text.RegularExpressions;
using AgentScope.Harness.Workspace;
using FsFileInfo = AgentScope.Harness.Filesystem.FileInfo;

namespace AgentScope.Harness.Filesystem.Local;

/// <summary>
/// 本地磁盘文件系统实现。对标 Java LocalFilesystem。
/// 支持三种路径隔离模式（Sandboxed/Rooted/Unrestricted）。
/// </summary>
public sealed class LocalFilesystem(string rootDir, LocalFsMode mode = LocalFsMode.Sandboxed,
    PathPolicy? policy = null) : IFilesystem
{
    private readonly PathPolicy _policy = policy ?? PathPolicy.FromWorkspace(rootDir);

    public async Task<ReadResult> ReadAsync(string filePath, int? offset = null, int? limit = null,
        CancellationToken ct = default)
    {
        var full = Resolve(filePath);
        if (!File.Exists(full)) return new ReadResult(null, false);

        var content = await File.ReadAllTextAsync(full, ct);
        if (offset.HasValue || limit.HasValue)
        {
            var lines = content.Split('\n');
            var start = offset ?? 0;
            var count = limit ?? lines.Length;
            content = string.Join('\n', lines.Skip(start).Take(count));
        }
        return new ReadResult(content, true);
    }

    public async Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default)
    {
        var full = Resolve(filePath);
        var dir = Path.GetDirectoryName(full);
        if (dir != null) Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(full, content, ct);
        return new WriteResult(true);
    }

    public async Task<EditResult> EditAsync(string filePath, string oldString, string newString,
        bool replaceAll = false, CancellationToken ct = default)
    {
        var read = await ReadAsync(filePath, ct: ct);
        if (!read.Found) return new EditResult(false, "文件不存在");

        var content = read.Content!;
        if (replaceAll)
            content = content.Replace(oldString, newString);
        else
        {
            var idx = content.IndexOf(oldString, StringComparison.Ordinal);
            if (idx < 0) return new EditResult(false, "未找到匹配文本");
            content = content[..idx] + newString + content[(idx + oldString.Length)..];
        }

        await WriteAsync(filePath, content, ct);
        return new EditResult(true);
    }

    public Task<LsResult> ListAsync(string path, CancellationToken ct = default)
    {
        var full = Resolve(path);
        if (!Directory.Exists(full))
            return Task.FromResult(new LsResult([], "目录不存在"));

        var files = Directory.GetFileSystemEntries(full)
            .Select(f => new FsFileInfo(
                Path.GetFileName(f), f,
                Directory.Exists(f),
                System.IO.File.Exists(f) ? new System.IO.FileInfo(f).Length : 0,
                System.IO.File.GetLastWriteTime(f)))
            .ToList();

        return Task.FromResult(new LsResult(files));
    }

    public Task<GlobResult> GlobAsync(string pattern, string? path = null, CancellationToken ct = default)
    {
        var searchPath = path != null ? Resolve(path) : rootDir;
        if (!Directory.Exists(searchPath))
            return Task.FromResult(new GlobResult([], "路径不存在"));

        var results = Directory.GetFiles(searchPath, pattern, SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(rootDir, f))
            .ToList();

        return Task.FromResult(new GlobResult(results));
    }

    public Task<GrepResult> GrepAsync(string pattern, string? path = null, string? glob = null,
        CancellationToken ct = default)
    {
        var searchPath = path != null ? Resolve(path) : rootDir;
        if (!Directory.Exists(searchPath))
            return Task.FromResult(new GrepResult([], "路径不存在"));

        var files = glob != null
            ? Directory.GetFiles(searchPath, glob, SearchOption.AllDirectories)
            : Directory.GetFiles(searchPath, "*", SearchOption.AllDirectories);

        var matches = new List<GrepMatch>();
        foreach (var file in files)
        {
            if (ct.IsCancellationRequested) break;
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (Regex.IsMatch(lines[i], pattern))
                    matches.Add(new GrepMatch(file, i + 1, lines[i]));
            }
        }

        return Task.FromResult(new GrepResult(matches));
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        var full = Resolve(path);
        return Task.FromResult(File.Exists(full) || Directory.Exists(full));
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var full = Resolve(path);
        if (File.Exists(full)) File.Delete(full);
        else if (Directory.Exists(full)) Directory.Delete(full, true);
        return Task.CompletedTask;
    }

    public Task MoveAsync(string from, string to, CancellationToken ct = default)
    {
        var fromFull = Resolve(from);
        var toFull = Resolve(to);
        var dir = Path.GetDirectoryName(toFull);
        if (dir != null) Directory.CreateDirectory(dir);
        File.Move(fromFull, toFull, overwrite: true);
        return Task.CompletedTask;
    }

    private string Resolve(string path)
    {
        if (Path.IsPathRooted(path))
        {
            if (mode == LocalFsMode.Unrestricted) return path;
            _policy.EnsureAllowed(path);
            return path;
        }

        var combined = Path.GetFullPath(Path.Combine(rootDir, path));
        _policy.EnsureAllowed(combined);
        return combined;
    }
}
