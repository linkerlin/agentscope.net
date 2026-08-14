namespace AgentScope.Core.Service.Discovery;

/// <summary>
/// Agent 注册接口（A2A SPI）。对标 Java AgentRegistry。
/// </summary>
public interface IAgentRegistry
{
    ValueTask RegisterAsync(AgentCard card, CancellationToken ct = default);
    ValueTask UnregisterAsync(string agentId, CancellationToken ct = default);
    ValueTask<AgentCard?> ResolveAsync(string agentId, CancellationToken ct = default);
    IAsyncEnumerable<AgentCard> ListAsync(CancellationToken ct = default);
}

/// <summary>
/// Agent 能力描述卡片
/// </summary>
public sealed record AgentCard(
    string AgentId,
    string Name,
    string Description,
    string Endpoint,
    string? Provider = null,
    IReadOnlyList<string>? Skills = null);
