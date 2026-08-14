using System.Collections.Concurrent;
using AgentScope.Core.Agent;

namespace AgentScope.Core.AgUI.Registry;

/// <summary>
/// AG-UI Agent 注册表。对标 Java AguiAgentRegistry。
/// 支持单例和工厂注册。
/// </summary>
public sealed class AguiAgentRegistry
{
    private readonly ConcurrentDictionary<string, IAgent> _singletons = new();
    private readonly ConcurrentDictionary<string, Func<IAgent>> _factories = new();

    public void Register(string agentId, IAgent agent) => _singletons[agentId] = agent;
    public void RegisterFactory(string agentId, Func<IAgent> factory) => _factories[agentId] = factory;

    public IAgent GetAgent(string agentId) =>
        _singletons.TryGetValue(agentId, out var a) ? a :
        _factories.TryGetValue(agentId, out var f) ? f() :
        throw new KeyNotFoundException($"AG-UI Agent '{agentId}' 未注册");

    public bool HasAgent(string agentId) =>
        _singletons.ContainsKey(agentId) || _factories.ContainsKey(agentId);

    public void Unregister(string agentId) { _singletons.TryRemove(agentId, out _); _factories.TryRemove(agentId, out _); }
    public void Clear() { _singletons.Clear(); _factories.Clear(); }
}
