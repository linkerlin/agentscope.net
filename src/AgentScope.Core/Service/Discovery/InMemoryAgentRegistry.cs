// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
