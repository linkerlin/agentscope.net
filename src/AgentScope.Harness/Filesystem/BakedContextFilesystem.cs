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
/// Baked context filesystem. Pre-loads context file content into memory and provides read-only access.
/// 烘焙上下文文件系统。将上下文文件内容预先加载到内存中，提供只读访问。
/// Counterpart to Java BakedContextFilesystem.
/// Used to "bake" context files (prompts, templates) into memory during Agent initialization.
/// </summary>
public sealed class BakedContextFilesystem : IFilesystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

    public BakedContextFilesystem()
    {
    }

    public BakedContextFilesystem(IReadOnlyDictionary<string, string> files)
    {
        foreach (var kv in files)
        {
            _files[kv.Key] = kv.Value;
        }
    }

    /// <summary>
    /// Add or update a baked file.
    /// 添加或更新一个烘焙文件。
    /// </summary>
    /// <param name="path">文件路径 / File path</param>
    /// <param name="content">文件内容 / File content</param>
    public void AddFile(string path, string content)
    {
        _files[path] = content;
    }

    /// <inheritdoc />
    public Task<ReadResult> ReadAsync(string filePath, int? offset = null, int? limit = null,
        CancellationToken ct = default)
    {
        if (!_files.TryGetValue(filePath, out var content))
        {
            return Task.FromResult(new ReadResult(null, false, "文件不存在"));
        }

        if (offset.HasValue || limit.HasValue)
        {
            var start = offset ?? 0;
            var len = limit ?? (content.Length - start);
            if (start < 0 || start >= content.Length)
            {
                return Task.FromResult(new ReadResult(null, false, "偏移量超出范围"));
            }

            content = content.Substring(start, Math.Min(len, content.Length - start));
        }

        return Task.FromResult(new ReadResult(content, true));
    }

    /// <inheritdoc />
    public Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default)
    {
        return Task.FromResult(new WriteResult(false, "烘焙上下文文件系统为只读"));
    }

    /// <inheritdoc />
    public Task<EditResult> EditAsync(string filePath, string oldString, string newString,
        bool replaceAll = false, CancellationToken ct = default)
    {
        return Task.FromResult(new EditResult(false, "烘焙上下文文件系统为只读"));
    }

    /// <inheritdoc />
    public Task<LsResult> ListAsync(string path, CancellationToken ct = default)
    {
        var prefix = path.TrimEnd('/');
        var files = _files.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(k => new FileInfo(
                Path.GetFileName(k),
                k,
                false,
                System.Text.Encoding.UTF8.GetByteCount(_files[k]),
                DateTime.MinValue))
            .ToList() as IReadOnlyList<FileInfo>;

        return Task.FromResult(new LsResult(files));
    }

    /// <inheritdoc />
    public Task<GlobResult> GlobAsync(string pattern, string? path = null, CancellationToken ct = default)
    {
        var regex = new System.Text.RegularExpressions.Regex(
            "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*") + "$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var matches = _files.Keys
            .Where(k => path == null || k.StartsWith(path, StringComparison.OrdinalIgnoreCase))
            .Where(k => regex.IsMatch(System.IO.Path.GetFileName(k)))
            .ToList() as IReadOnlyList<string>;

        return Task.FromResult(new GlobResult(matches ?? Array.Empty<string>()));
    }

    /// <inheritdoc />
    public Task<GrepResult> GrepAsync(string pattern, string? path = null, string? glob = null,
        CancellationToken ct = default)
    {
        var matches = new List<GrepMatch>();
        foreach (var (filePath, content) in _files)
        {
            if (path != null && !filePath.StartsWith(path, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var lines = content.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(new GrepMatch(filePath, i + 1, lines[i].TrimEnd('\r')));
                }
            }
        }

        return Task.FromResult(new GrepResult(matches));
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        return Task.FromResult(_files.ContainsKey(path));
    }

    /// <inheritdoc />
    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        _files.Remove(path);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MoveAsync(string from, string to, CancellationToken ct = default)
    {
        if (_files.TryGetValue(from, out var content))
        {
            _files.Remove(from);
            _files[to] = content;
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }
}
