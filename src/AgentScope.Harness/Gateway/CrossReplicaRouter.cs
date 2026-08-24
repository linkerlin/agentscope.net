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

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 跨副本路由。对标 Java cross-replica routing 逻辑。
/// 根据子 Agent 的 session 信息将请求路由到正确的副本。
/// </summary>
public sealed class CrossReplicaRouter(StoreBackedSubagentRegistry registry)
{
    /// <summary>
    /// 将子 Agent ID 解析为可用的端点地址（本地优先）。
    /// Resolve the subagent ID to an available endpoint address (local first).
    /// </summary>
    /// <param name="subagentId">子 Agent ID / The subagent ID.</param>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    /// <returns>端点 URL，未找到时返回 null / The endpoint URL, or null if not found.</returns>
    public async Task<string?> ResolveEndpointAsync(string subagentId, CancellationToken ct = default)
    {
        var record = await registry.ResolveAsync(subagentId, ct);
        return record?.Endpoint;
    }

    /// <summary>
    /// 检查子 Agent 是否在本副本本地注册。
    /// Check if the subagent is registered locally on this replica.
    /// </summary>
    /// <param name="subagentId">子 Agent ID / The subagent ID.</param>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    /// <returns>若在本地则返回 true / True if local.</returns>
    public async Task<bool> IsLocalAsync(string subagentId, CancellationToken ct = default)
    {
        var record = await registry.ResolveAsync(subagentId, ct);
        return record != null;
    }

    /// <summary>
    /// 恢复指定父会话下的全部子 Agent 列表。
    /// Restore all subagents under the specified parent session.
    /// </summary>
    /// <param name="sessionId">父会话 ID / The parent session ID.</param>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    /// <returns>子 Agent ID 列表 / List of subagent IDs.</returns>
    public async Task<IReadOnlyList<string>> RestoreSessionSubagentsAsync(string sessionId,
        CancellationToken ct = default)
    {
        var records = await registry.RestoreSessionAsync(sessionId, ct);
        return records.Select(r => r.SubagentId).ToList();
    }
}
