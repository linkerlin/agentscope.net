using System.Collections.Concurrent;
using AgentScope.Core.Agent;

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 跨副本子 Agent 注册表（支持 Session 恢复）。对标 Java StoreBackedSubagentRegistry。
/// subagent 注册信息通过 IDistributedStore 持久化，支持跨副本路由和 session 恢复。
/// </summary>
public sealed class StoreBackedSubagentRegistry
{
    private readonly IDistributedStore _store;
    private readonly ConcurrentDictionary<string, SubagentRecord> _localCache = new();

    public StoreBackedSubagentRegistry(IDistributedStore store)
    {
        _store = store;
    }

    /// <summary>注册子 Agent 到本地缓存和持久化存储</summary>
    public async Task RegisterAsync(string subagentId, string parentSessionId, string agentName, string endpoint,
        CancellationToken ct = default)
    {
        var record = new SubagentRecord(subagentId, parentSessionId, agentName, endpoint);
        _localCache[subagentId] = record;

        var json = System.Text.Json.JsonSerializer.Serialize(record);
        await _store.SetAsync($"subagent:{subagentId}", json, ct: ct);
        await _store.SetAsync($"session:{parentSessionId}:subagents",
            (await _store.GetAsync($"session:{parentSessionId}:subagents", ct) ?? "") + "," + subagentId,
            ct: ct);
    }

    /// <summary>查找子 Agent（本地 → 远程）</summary>
    public async Task<SubagentRecord?> ResolveAsync(string subagentId, CancellationToken ct = default)
    {
        if (_localCache.TryGetValue(subagentId, out var local))
            return local;

        var json = await _store.GetAsync($"subagent:{subagentId}", ct);
        if (json == null) return null;

        var record = System.Text.Json.JsonSerializer.Deserialize<SubagentRecord>(json);
        if (record != null) _localCache[subagentId] = record;
        return record;
    }

    /// <summary>按父会话恢复所有子 Agent</summary>
    public async Task<IReadOnlyList<SubagentRecord>> RestoreSessionAsync(string parentSessionId,
        CancellationToken ct = default)
    {
        var json = await _store.GetAsync($"session:{parentSessionId}:subagents", ct);
        if (string.IsNullOrEmpty(json)) return [];

        var ids = json.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<SubagentRecord>();
        foreach (var id in ids)
        {
            var record = await ResolveAsync(id.Trim(), ct);
            if (record != null) results.Add(record);
        }
        return results;
    }

    /// <summary>收回子 Agent</summary>
    public async Task RevokeAsync(string subagentId, CancellationToken ct = default)
    {
        _localCache.TryRemove(subagentId, out _);
        await _store.DeleteAsync($"subagent:{subagentId}", ct);
    }
}
