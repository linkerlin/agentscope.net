using System.Net.Http.Json;
using System.Text.Json;
using AgentScope.Core.Service.Discovery;

namespace AgentScope.Extensions.Nacos;

/// <summary>
/// Nacos Agent Card 解析器（HTTP API 实现）。对标 Java NacosAgentCardResolver。
/// </summary>
public sealed class NacosAgentCardResolver(
    HttpClient httpClient,
    string serverAddr = "http://localhost:8848",
    string groupName = "DEFAULT_GROUP")
{
    public async Task<AgentCard?> ResolveAsync(string agentName, CancellationToken ct = default)
    {
        var url = $"{serverAddr}/nacos/v1/ns/instance/list?" +
                  $"serviceName={agentName}&groupName={groupName}&healthyOnly=true";
        using var resp = await httpClient.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (!json.TryGetProperty("hosts", out var hosts) || hosts.GetArrayLength() == 0)
            return null;

        var inst = hosts[0];
        var ip = inst.GetProperty("ip").GetString() ?? "localhost";
        var port = inst.GetProperty("port").GetInt32();
        var meta = inst.TryGetProperty("metadata", out var m) ? m : default;

        return new AgentCard(
            agentName,
            inst.TryGetProperty("serviceName", out var sn) ? sn.GetString() ?? agentName : agentName,
            meta.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
            $"{ip}:{port}",
            meta.TryGetProperty("provider", out var p) ? p.GetString() : null);
    }
}
