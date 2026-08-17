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

    /// <summary>
    /// 初始化基于分布式存储的跨副本子 Agent 注册表。
    /// Initialize the cross-replica subagent registry backed by a distributed store.
    /// </summary>
    /// <param name="store">分布式存储接口 / The distributed store interface.</param>
    public StoreBackedSubagentRegistry(IDistributedStore store)
    {
        _store = store;
    }

    /// <summary>
    /// 注册子 Agent 到本地缓存和分布式持久化存储。
    /// Register a subagent into the local cache and distributed persistent store.
    /// </summary>
    /// <param name="subagentId">子 Agent ID / The subagent ID.</param>
    /// <param name="parentSessionId">父会话 ID / The parent session ID.</param>
    /// <param name="agentName">Agent 名称 / The agent name.</param>
    /// <param name="endpoint">端点地址 / The endpoint address.</param>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
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

    /// <summary>
    /// 查找子 Agent（先查本地缓存，再查分布式存储）。
    /// Resolve a subagent (local cache first, then distributed store).
    /// </summary>
    /// <param name="subagentId">子 Agent ID / The subagent ID.</param>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    /// <returns>子 Agent 记录，未找到时返回 null / The subagent record, or null if not found.</returns>
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

    /// <summary>
    /// 按父会话 ID 恢复该会话下所有子 Agent 记录。
    /// Restore all subagent records under the given parent session ID.
    /// </summary>
    /// <param name="parentSessionId">父会话 ID / The parent session ID.</param>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    /// <returns>子 Agent 记录列表 / List of subagent records.</returns>
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

    /// <summary>
    /// 收回（注销）指定子 Agent，从本地缓存和分布式存储中移除。
    /// Revoke (unregister) the specified subagent from local cache and distributed store.
    /// </summary>
    /// <param name="subagentId">子 Agent ID / The subagent ID.</param>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    public async Task RevokeAsync(string subagentId, CancellationToken ct = default)
    {
        _localCache.TryRemove(subagentId, out _);
        await _store.DeleteAsync($"subagent:{subagentId}", ct);
    }
}
