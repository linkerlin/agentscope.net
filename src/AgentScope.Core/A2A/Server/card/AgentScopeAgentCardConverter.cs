using AgentScope.Core.Service.Discovery;

namespace AgentScope.Core.A2A.Server.Card;

/// <summary>
/// 将 ConfigurableAgentCard 转换为 A2A AgentCard。对标 Java AgentScopeAgentCardConverter。
/// </summary>
public static class AgentScopeAgentCardConverter
{
    public static AgentCard Convert(ConfigurableAgentCard config) =>
        config.Build();
}
