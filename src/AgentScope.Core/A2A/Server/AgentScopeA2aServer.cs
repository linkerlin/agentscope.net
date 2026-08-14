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
