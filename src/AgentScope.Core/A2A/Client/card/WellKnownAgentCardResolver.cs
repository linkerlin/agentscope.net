using System.Net.Http.Json;
using AgentScope.Core.Service.Discovery;

namespace AgentScope.Core.A2A.Client.Card;

/// <summary>
/// Well-Known AgentCard 解析器。对标 Java WellKnownAgentCardResolver。
/// 从 /.well-known/agent-card.json 获取卡片。
/// </summary>
public sealed class WellKnownAgentCardResolver(HttpClient http) : IAgentCardResolver
{
    public async Task<AgentCard?> ResolveAsync(string agentName, CancellationToken ct = default)
    {
        var baseUrl = agentName.StartsWith("http") ? agentName : $"https://{agentName}";
        var uri = $"{baseUrl.TrimEnd('/')}/.well-known/agent-card.json";
        try
        {
            return await http.GetFromJsonAsync<AgentCard>(uri, ct);
        }
        catch
        {
            return null;
        }
    }
}
