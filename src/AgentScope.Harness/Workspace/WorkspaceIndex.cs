using System.Collections.Concurrent;

namespace AgentScope.Harness.Workspace;

/// <summary>SQLite 文件索引，对应 Java WorkspaceIndex</summary>
public sealed class WorkspaceIndex
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _index = new();

    public void Add(string filePath, string? tag = null)
    {
        var key = tag ?? "default";
        _index.AddOrUpdate(key,
            _ => new HashSet<string> { filePath },
            (_, set) => { set.Add(filePath); return set; });
    }

    public void Remove(string filePath, string? tag = null)
    {
        var key = tag ?? "default";
        if (_index.TryGetValue(key, out var set))
            set.Remove(filePath);
    }

    public IReadOnlySet<string> GetByTag(string? tag = null)
    {
        var key = tag ?? "default";
        return _index.TryGetValue(key, out var set)
            ? set.ToHashSet()
            : new HashSet<string>();
    }

    public bool Contains(string filePath)
    {
        return _index.Values.Any(set => set.Contains(filePath));
    }

    public void Clear() => _index.Clear();
}
