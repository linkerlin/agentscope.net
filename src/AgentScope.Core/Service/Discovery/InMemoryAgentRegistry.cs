using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AgentScope.Core.Service.Discovery;

/// <summary>
/// 进程内 Agent 注册表。对标 Java InMemoryAgentRegistry。
/// </summary>
public sealed class InMemoryAgentRegistry : IAgentRegistry
{
    private readonly ConcurrentDictionary<string, AgentCard> _cards = new();

    public ValueTask RegisterAsync(AgentCard card, CancellationToken ct = default)
    {
        _cards[card.AgentId] = card;
        return ValueTask.CompletedTask;
    }

    public ValueTask UnregisterAsync(string agentId, CancellationToken ct = default)
    {
        _cards.TryRemove(agentId, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask<AgentCard?> ResolveAsync(string agentId, CancellationToken ct = default) =>
        ValueTask.FromResult(_cards.TryGetValue(agentId, out var card) ? card : null);

    public async IAsyncEnumerable<AgentCard> ListAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var card in _cards.Values)
        {
            ct.ThrowIfCancellationRequested();
            yield return card;
        }
    }
}
