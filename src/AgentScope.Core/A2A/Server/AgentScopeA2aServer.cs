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

using AgentScope.Core.A2A.Server.Card;
using AgentScope.Core.A2A.Server.Executor;
using AgentScope.Core.A2A.Server.Executor.Runner;
using AgentScope.Core.A2A.Server.Transport;
using AgentScope.Core.Service.Discovery;

namespace AgentScope.Core.A2A.Server;

/// <summary>
/// A2A Server 入口点。对标 Java AgentScopeA2aServer。
/// 组装 AgentCard、TransportWrapper、AgentExecutor、AgentRegistry。
/// </summary>
public sealed class AgentScopeA2aServer
{
    private readonly AgentCard _card;
    private readonly ITransportWrapper _transport;
    private readonly AgentScopeAgentExecutor _executor;
    private readonly List<IAgentRegistry> _registries = [];

    public AgentScopeA2aServer(IAgentRunner runner, ConfigurableAgentCard? cardConfig = null)
    {
        _card = (cardConfig ?? new ConfigurableAgentCard()).Build();
        _executor = new AgentScopeAgentExecutor(runner);
        _transport = new Transport.JsonRpc.JsonRpcTransportWrapper(_executor, runner);
    }

    public void AddRegistry(IAgentRegistry registry) => _registries.Add(registry);

    public async Task PostEndpointReadyAsync(CancellationToken ct = default)
    {
        foreach (var registry in _registries)
            await registry.RegisterAsync(_card, ct);
    }

    public async Task<object> HandleRequestAsync(string body, IDictionary<string, string>? headers = null,
        CancellationToken ct = default) =>
        await _transport.HandleRequestAsync(body, headers, ct);
}
