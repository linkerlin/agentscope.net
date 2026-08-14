using System.Collections.Concurrent;

namespace AgentScope.Core.Service.Discovery;

/// <summary>
/// 数据面注册表。端口 aistio registry.go。
/// 内存优先、Nacos 后置，负责 Agent 的自注册/心跳/失效发现。
/// </summary>
public sealed class DataPlaneRegistry(TimeSpan? staleAfter = null)
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new();
    private readonly TimeSpan _stale = staleAfter ?? TimeSpan.FromSeconds(30);

    public void Upsert(string id, AgentSummary summary) =>
        _entries.AddOrUpdate(id,
            _ => new Entry(summary, DateTime.UtcNow),
            (_, e) => { e.Summary = summary; e.LastHeartbeat = DateTime.UtcNow; return e; });

    public void Heartbeat(string id)
    {
        if (_entries.TryGetValue(id, out var e))
            e.LastHeartbeat = DateTime.UtcNow;
    }

    public IReadOnlyList<AgentSummary> ListByAgent(string agent) =>
        _entries.Values
            .Where(e => e.Summary.Agent == agent && !IsStale(e))
            .Select(e => e.Summary)
            .ToList();

    public void MarkStale(string id) => _entries.TryRemove(id, out _);

    public int ActiveCount => _entries.Count(e => !IsStale(e.Value));

    public TimeSpan StaleAfter => _stale;

    public bool IsStale(Entry e) => DateTime.UtcNow - e.LastHeartbeat > _stale;

    public IReadOnlyList<KeyValuePair<string, Entry>> GetAllEntries() =>
        _entries.ToList();

    public sealed class Entry(AgentSummary Summary, DateTime LastHeartbeat)
    {
        public AgentSummary Summary { get; set; } = Summary;
        public DateTime LastHeartbeat { get; set; } = LastHeartbeat;
    }
}

/// <summary>
/// Agent 摘要（注册表条目）。对标 aistio registry.Entry。
/// </summary>
public readonly record struct AgentSummary(
    string Agent,
    string Runtime,
    string Endpoint,
    string ContractLevel);
