using AgentScope.Core.Service.Discovery;

namespace AgentScope.Core.A2A.Client.Card;

/// <summary>
/// AgentCard 解析器接口。对标 Java AgentCardResolver。
/// </summary>
public interface IAgentCardResolver
{
    Task<AgentCard?> ResolveAsync(string agentName, CancellationToken ct = default);
}
