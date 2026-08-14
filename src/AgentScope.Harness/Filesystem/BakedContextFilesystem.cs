// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Harness.Filesystem;

/// <summary>
/// 烘焙上下文文件系统。将上下文文件内容预先加载到内存中，提供只读访问。
/// 对标 Java BakedContextFilesystem。
/// 用于在 Agent 初始化时将提示词、模板等上下文文件"烘焙"到内存中。
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
    /// 添加或更新一个烘焙文件。
    /// </summary>
    public void AddFile(string path, string content)
    {
        _files[path] = content;
    }

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

    public Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default)
    {
        return Task.FromResult(new WriteResult(false, "烘焙上下文文件系统为只读"));
    }

    public Task<EditResult> EditAsync(string filePath, string oldString, string newString,
        bool replaceAll = false, CancellationToken ct = default)
    {
        return Task.FromResult(new EditResult(false, "烘焙上下文文件系统为只读"));
    }

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

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        return Task.FromResult(_files.ContainsKey(path));
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        _files.Remove(path);
        return Task.CompletedTask;
    }

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
