namespace AgentScope.Harness.Gateway;

/// <summary>
/// 跨副本路由。对标 Java cross-replica routing 逻辑。
/// 根据子 Agent 的 session 信息将请求路由到正确的副本。
/// </summary>
public sealed class CrossReplicaRouter(StoreBackedSubagentRegistry registry)
{
    /// <summary>将消息路由到正确的子 Agent 端点</summary>
    public async Task<string?> ResolveEndpointAsync(string subagentId, CancellationToken ct = default)
    {
        var record = await registry.ResolveAsync(subagentId, ct);
        return record?.Endpoint;
    }

    /// <summary>检查子 Agent 是否在当前副本本地</summary>
    public async Task<bool> IsLocalAsync(string subagentId, CancellationToken ct = default)
    {
        var record = await registry.ResolveAsync(subagentId, ct);
        return record != null;
    }

    /// <summary>恢复会话的全部子 Agent</summary>
    public async Task<IReadOnlyList<string>> RestoreSessionSubagentsAsync(string sessionId,
        CancellationToken ct = default)
    {
        var records = await registry.RestoreSessionAsync(sessionId, ct);
        return records.Select(r => r.SubagentId).ToList();
    }
}
