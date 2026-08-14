using System.Collections.Concurrent;
using AgentScope.Core.Agent;

namespace AgentScope.Harness.Subagent;

/// <summary>
/// 默认子 Agent 管理器。对标 Java DefaultAgentManager。
/// </summary>
public sealed class DefaultAgentManager : ISubagentManager
{
    private readonly ConcurrentDictionary<string, IAgent> _registry = new(StringComparer.OrdinalIgnoreCase);
    private readonly SubagentFactory? _factory;

    public DefaultAgentManager(SubagentFactory? factory = null)
    {
        _factory = factory;
    }

    public IAgent GetOrCreate(string specRef)
    {
        if (_registry.TryGetValue(specRef, out var existing))
            return existing;

        IAgent agent;
        if (_factory != null)
        {
            var decl = AgentSpecLoader.Load(specRef);
            agent = _factory(decl);
        }
        else
        {
            throw new InvalidOperationException($"未注册子 Agent: {specRef}");
        }

        _registry[specRef] = agent;
        return agent;
    }

    public void Register(string name, IAgent agent) => _registry[name] = agent;
    public void Remove(string name) => _registry.TryRemove(name, out _);
}
