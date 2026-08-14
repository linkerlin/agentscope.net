using System.Collections.Concurrent;

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 子 Agent 注册表。对标 Java SubagentRegistry/InMemorySubagentRegistry。
/// </summary>
public sealed class SubagentRegistry
{
    private readonly ConcurrentDictionary<string, SubagentRecord> _records = new();

    public void Register(SubagentRecord record) => _records[record.SubagentId] = record;
    public SubagentRecord? Find(string subagentId) =>
        _records.TryGetValue(subagentId, out var r) ? r : null;
    public void Revoke(string subagentId) => _records.TryRemove(subagentId, out _);
    public IReadOnlyList<SubagentRecord> ListByParent(string parentSessionId) =>
        _records.Values.Where(r => r.ParentSessionId == parentSessionId).ToList();
}

/// <summary>
/// 子 Agent 注册记录。对标 Java SubagentRecord。
/// </summary>
public sealed record SubagentRecord(
    string SubagentId,
    string ParentSessionId,
    string AgentName,
    string Endpoint,
    DateTime CreatedAt = default)
{
    public DateTime CreatedAt { get; init; } = CreatedAt == default ? DateTime.UtcNow : CreatedAt;
}
