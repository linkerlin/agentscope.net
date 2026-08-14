using AgentScope.Core.Service.Discovery;

namespace AgentScope.Core.A2A.Server.Card;

/// <summary>
/// 可配置的 AgentCard Builder。对标 Java ConfigurableAgentCard。
/// </summary>
public sealed class ConfigurableAgentCard
{
    public string Name { get; set; } = "a2a-agent";
    public string Description { get; set; } = "A2A Agent";
    public string? Url { get; set; }
    public string? Provider { get; set; }
    public List<string> Skills { get; set; } = [];
    public bool Streaming { get; set; } = true;

    public AgentCard Build() => new(
        Guid.NewGuid().ToString(),
        Name, Description,
        Url ?? $"http://localhost:5000",
        Provider,
        Skills);
}
