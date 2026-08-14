namespace AgentScope.Core.Service.Discovery;

/// <summary>
/// 控制面服务。对标 Java aistio control plane (registry.go + control_plane.go)。
/// 提供 Agent 注册、心跳、发现、Dashboard 统计的聚合入口。
/// </summary>
public sealed class ControlPlaneService
{
    private readonly DataPlaneRegistry _dataPlane;
    private readonly IAgentRegistry _agentRegistry;

    public ControlPlaneService(DataPlaneRegistry dataPlane, IAgentRegistry agentRegistry)
    {
        _dataPlane = dataPlane;
        _agentRegistry = agentRegistry;
    }

    public async Task RegisterAsync(AgentCard card, CancellationToken ct = default)
    {
        await _agentRegistry.RegisterAsync(card, ct);
        _dataPlane.Upsert(card.AgentId, new AgentSummary(card.Name, card.Provider ?? "unknown", card.Endpoint, "v3"));
    }

    public void Heartbeat(string agentId) => _dataPlane.Heartbeat(agentId);
    public void MarkStale(string agentId) => _dataPlane.MarkStale(agentId);

    public IReadOnlyList<AgentSummary> ListByAgent(string agent) =>
        _dataPlane.ListByAgent(agent);

    public DashboardStats GetStats()
    {
        var all = _dataPlane.GetAllEntries();
        return new DashboardStats
        {
            TotalAgents = all.Count,
            ActiveAgents = all.Count(e => !_dataPlane.IsStale(e.Value)),
            StaleAgents = all.Count(e => _dataPlane.IsStale(e.Value)),
            ByRuntime = all.GroupBy(e => e.Value.Summary.Runtime)
                .Select(g => new AgentGroup(g.Key, g.Count())).ToList(),
            ByContractLevel = all.GroupBy(e => e.Value.Summary.ContractLevel)
                .Select(g => new AgentGroup(g.Key, g.Count())).ToList()
        };
    }
}
