using AgentScope.Core.Service.Discovery;

namespace AgentScope.Core.A2A.Client.Card;

/// <summary>
/// 固定 AgentCard 解析器。对标 Java FixedAgentCardResolver。
/// 所有名称返回同一张卡片。
/// </summary>
public sealed class FixedAgentCardResolver(AgentCard card) : IAgentCardResolver
{
    public Task<AgentCard?> ResolveAsync(string agentName, CancellationToken ct = default) =>
        Task.FromResult<AgentCard?>(card);
}
