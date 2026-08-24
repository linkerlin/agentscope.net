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

namespace AgentScope.Core.Service.Discovery;

/// <summary>
/// Data plane registry. Port of aistio registry.go.
/// Memory-first, Nacos-fallback, responsible for agent self-registration, heartbeat, and stale detection.
/// 数据面注册表。端口 aistio registry.go。
/// 内存优先、Nacos 后置，负责 Agent 的自注册/心跳/失效发现。
/// </summary>
public sealed class DataPlaneRegistry(TimeSpan? staleAfter = null)
{
    /// <summary>
    /// Concurrent dictionary storing all registered agent entries.
    /// 存储所有已注册 Agent 条目的并发字典。
    /// </summary>
    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    /// <summary>
    /// Time duration after which an agent is considered stale without heartbeat.
    /// Agent 在无心跳后被判定为失效的时间间隔。
    /// </summary>
    private readonly TimeSpan _stale = staleAfter ?? TimeSpan.FromSeconds(30);

    /// <summary>
    /// Inserts or updates an agent entry in the registry.
    /// 在注册表中插入或更新一个 Agent 条目。
    /// </summary>
    public void Upsert(string id, AgentSummary summary) =>
        _entries.AddOrUpdate(id,
            _ => new Entry(summary, DateTime.UtcNow),
            (_, e) => { e.Summary = summary; e.LastHeartbeat = DateTime.UtcNow; return e; });

    /// <summary>
    /// Records a heartbeat for the specified agent, updating its last heartbeat timestamp.
    /// 记录指定 Agent 的心跳，更新其最后心跳时间戳。
    /// </summary>
    public void Heartbeat(string id)
    {
        if (_entries.TryGetValue(id, out var e))
            e.LastHeartbeat = DateTime.UtcNow;
    }

    /// <summary>
    /// Lists all active (non-stale) agent summaries for a given agent name.
    /// 列出指定 Agent 名称的所有活跃（非失效）Agent 摘要。
    /// </summary>
    public IReadOnlyList<AgentSummary> ListByAgent(string agent) =>
        _entries.Values
            .Where(e => e.Summary.Agent == agent && !IsStale(e))
            .Select(e => e.Summary)
            .ToList();

    /// <summary>
    /// Marks an agent as stale by removing it from the registry.
    /// 通过从注册表中移除来将 Agent 标记为失效。
    /// </summary>
    public void MarkStale(string id) => _entries.TryRemove(id, out _);

    /// <summary>
    /// Gets the count of active (non-stale) agents.
    /// 获取活跃（非失效）Agent 的数量。
    /// </summary>
    public int ActiveCount => _entries.Count(e => !IsStale(e.Value));

    /// <summary>
    /// Gets the time duration after which an agent is considered stale.
    /// 获取 Agent 被判定为失效的时间间隔。
    /// </summary>
    public TimeSpan StaleAfter => _stale;

    /// <summary>
    /// Determines whether an agent entry is stale based on its last heartbeat.
    /// 根据最后心跳时间判断 Agent 条目是否失效。
    /// </summary>
    public bool IsStale(Entry e) => DateTime.UtcNow - e.LastHeartbeat > _stale;

    /// <summary>
    /// Gets all registry entries as a read-only list.
    /// 获取所有注册表条目为只读列表。
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, Entry>> GetAllEntries() =>
        _entries.ToList();

    /// <summary>
    /// Represents a single entry in the data plane registry with agent summary and heartbeat timestamp.
    /// 表示数据面注册表中的单个条目，包含 Agent 摘要和心跳时间戳。
    /// </summary>
    public sealed class Entry(AgentSummary Summary, DateTime LastHeartbeat)
    {
        /// <summary>
        /// Agent summary information.
        /// Agent 摘要信息。
        /// </summary>
        public AgentSummary Summary { get; set; } = Summary;

        /// <summary>
        /// Last heartbeat timestamp in UTC.
        /// 最后心跳的 UTC 时间戳。
        /// </summary>
        public DateTime LastHeartbeat { get; set; } = LastHeartbeat;
    }
}

/// <summary>
/// Agent summary (registry entry). Corresponds to aistio registry.Entry.
/// Agent 摘要（注册表条目）。对标 aistio registry.Entry。
/// </summary>
public readonly record struct AgentSummary(
    string Agent,
    string Runtime,
    string Endpoint,
    string ContractLevel);
