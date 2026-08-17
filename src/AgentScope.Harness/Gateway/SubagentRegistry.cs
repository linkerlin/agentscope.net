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

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 子 Agent 注册表。对标 Java SubagentRegistry/InMemorySubagentRegistry。
/// </summary>
public sealed class SubagentRegistry
{
    private readonly ConcurrentDictionary<string, SubagentRecord> _records = new();

    /// <summary>
    /// 注册一个子 Agent 记录。
    /// Register a subagent record.
    /// </summary>
    /// <param name="record">子 Agent 记录 / The subagent record.</param>
    public void Register(SubagentRecord record) => _records[record.SubagentId] = record;

    /// <summary>
    /// 按 ID 查找子 Agent 记录。
    /// Find a subagent record by ID.
    /// </summary>
    /// <param name="subagentId">子 Agent ID / The subagent ID.</param>
    /// <returns>子 Agent 记录，未找到时返回 null / The subagent record, or null if not found.</returns>
    public SubagentRecord? Find(string subagentId) =>
        _records.TryGetValue(subagentId, out var r) ? r : null;

    /// <summary>
    /// 收回（移除）一个子 Agent 记录。
    /// Revoke (remove) a subagent record.
    /// </summary>
    /// <param name="subagentId">子 Agent ID / The subagent ID.</param>
    public void Revoke(string subagentId) => _records.TryRemove(subagentId, out _);

    /// <summary>
    /// 按父会话 ID 列出所有关联的子 Agent 记录。
    /// List all subagent records associated with a parent session ID.
    /// </summary>
    /// <param name="parentSessionId">父会话 ID / The parent session ID.</param>
    /// <returns>子 Agent 记录列表 / List of subagent records.</returns>
    public IReadOnlyList<SubagentRecord> ListByParent(string parentSessionId) =>
        _records.Values.Where(r => r.ParentSessionId == parentSessionId).ToList();
}

/// <summary>
/// 子 Agent 注册记录。对标 Java SubagentRecord。
/// Subagent registration record. Counterpart to Java SubagentRecord.
/// </summary>
/// <param name="SubagentId">子 Agent 唯一标识 / The unique subagent identifier.</param>
/// <param name="ParentSessionId">父会话 ID / The parent session ID.</param>
/// <param name="AgentName">Agent 名称 / The agent name.</param>
/// <param name="Endpoint">端点地址 / The endpoint address.</param>
/// <param name="CreatedAt">创建时间 / The creation timestamp.</param>
public sealed record SubagentRecord(
    string SubagentId,
    string ParentSessionId,
    string AgentName,
    string Endpoint,
    DateTime CreatedAt = default)
{
    /// <summary>创建时间（默认 UTC 当前时间）/ Creation timestamp (defaults to UTC now).</summary>
    public DateTime CreatedAt { get; init; } = CreatedAt == default ? DateTime.UtcNow : CreatedAt;
}
