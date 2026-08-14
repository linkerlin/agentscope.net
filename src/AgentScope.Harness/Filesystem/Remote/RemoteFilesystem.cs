using System.Collections.Concurrent;
using System.Text.RegularExpressions;
namespace AgentScope.Harness.Filesystem.Remote;

public sealed class RemoteFilesystem : IFilesystem
{
    private readonly ConcurrentDictionary<string, string> _store = new();

    public Task<ReadResult> ReadAsync(string filePath, int? offset = null, int? limit = null, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(filePath, out var content))
            return Task.FromResult(new ReadResult(null, false, "File not found"));
        if (offset.HasValue || limit.HasValue)
        {
            var start = offset ?? 0;
            var len = limit ?? content.Length - start;
            content = start < content.Length ? content.Substring(start, Math.Min(len, content.Length - start)) : "";
        }
        return Task.FromResult(new ReadResult(content, true));
    }

    public Task<WriteResult> WriteAsync(string filePath, string content, CancellationToken ct = default)
    {
        _store[filePath] = content;
        return Task.FromResult(new WriteResult(true));
    }

    public Task<EditResult> EditAsync(string filePath, string oldString, string newString, bool replaceAll = false, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(filePath, out var content))
            return Task.FromResult(new EditResult(false, "File not found"));
        var updated = replaceAll ? content.Replace(oldString, newString) : content.Replace(oldString, newString, StringComparison.Ordinal);
        if (updated == content)
            return Task.FromResult(new EditResult(false, "Pattern not found"));
        _store[filePath] = updated;
        return Task.FromResult(new EditResult(true));
    }

    public Task<LsResult> ListAsync(string path, CancellationToken ct = default)
    {
        // 对标 Java RemoteFilesystem.ls：按前缀分组出子目录与文件
        var normalized = NormalizeDir(path);
        var subdirs = new HashSet<string>();
        var files = new List<FileInfo>();

        foreach (var key in _store.Keys)
        {
            if (!key.StartsWith(normalized, StringComparison.Ordinal)) continue;
            var rest = key[normalized.Length..];
            var slash = rest.IndexOf('/');
            if (slash < 0)
            {
                files.Add(new FileInfo(rest, key, false, _store[key].Length, DateTime.MinValue));
            }
            else
            {
                subdirs.Add(rest[..slash]);
            }
        }

        var all = subdirs.Select(d => new FileInfo(d, normalized + d, true, 0, DateTime.MinValue))
            .Concat(files)
            .OrderBy(f => f.Name)
            .ToList();
        return Task.FromResult(new LsResult(all));
    }

    public Task<GlobResult> GlobAsync(string pattern, string? path = null, CancellationToken ct = default)
    {
        // 对标 Java RemoteFilesystem.glob：将 glob 转换为正则匹配存储键
        var basePath = path != null ? NormalizeDir(path) : "";
        var regex = GlobToRegex(pattern);
        var matches = _store.Keys
            .Where(k => k.StartsWith(basePath, StringComparison.Ordinal) && regex.IsMatch(basePath.Length > 0 ? k[basePath.Length..] : k))
            .OrderBy(k => k)
            .ToList();
        return Task.FromResult(new GlobResult(matches));
    }

    public Task<GrepResult> GrepAsync(string pattern, string? path = null, string? glob = null, CancellationToken ct = default)
    {
        // 对标 Java RemoteFilesystem.grep：逐行匹配内容
        var regex = new Regex(pattern, RegexOptions.None);
        var basePath = path != null ? NormalizeDir(path) : "";
        var globRegex = glob != null ? GlobToRegex(glob) : null;

        var matches = new List<GrepMatch>();
        foreach (var kv in _store)
        {
            if (!kv.Key.StartsWith(basePath, StringComparison.Ordinal)) continue;
            var rel = basePath.Length > 0 ? kv.Key[basePath.Length..] : kv.Key;
            if (globRegex != null && !globRegex.IsMatch(rel)) continue;

            var lines = kv.Value.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (regex.IsMatch(lines[i]))
                    matches.Add(new GrepMatch(kv.Key, i + 1, lines[i]));
            }
        }
        return Task.FromResult(new GrepResult(matches));
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
        => Task.FromResult(_store.ContainsKey(path));

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        _store.TryRemove(path, out _);
        return Task.CompletedTask;
    }

    public Task MoveAsync(string from, string to, CancellationToken ct = default)
    {
        if (_store.TryRemove(from, out var content))
            _store[to] = content;
        return Task.CompletedTask;
    }

    private static string NormalizeDir(string path)
    {
        var p = path.TrimEnd('/');
        return p.Length == 0 ? "" : p + "/";
    }

    private static Regex GlobToRegex(string glob)
    {
        var sb = new System.Text.StringBuilder("^");
        for (var i = 0; i < glob.Length; i++)
        {
            var c = glob[i];
            switch (c)
            {
                case '*':
                    // 支持 ** 匹配任意层级
                    if (i + 1 < glob.Length && glob[i + 1] == '*')
                    {
                        sb.Append(".*");
                        i++;
                    }
                    else
                    {
                        sb.Append("[^/]*");
                    }
                    break;
                case '?':
                    sb.Append("[^/]");
                    break;
                case '.':
                case '(':
                case ')':
                case '+':
                case '|':
                case '^':
                case '$':
                case '{':
                case '}':
                case '[':
                case ']':
                case '\\':
                    sb.Append('\\').Append(c);
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        sb.Append('$');
        return new Regex(sb.ToString());
    }
}
