using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AgentScope.Core.Service.Discovery;

namespace AgentScope.Extensions.Nacos;

/// <summary>
/// Nacos Agent 注册表（HTTP API 实现）。对标 Java NacosAgentRegistry / NacosA2aRegistry。
/// 通过 Nacos Open API 注册/发现 Agent，不依赖 nacos-sdk-csharp 的具体版本。
/// </summary>
public sealed class NacosAgentRegistry(
    HttpClient httpClient,
    string serverAddr = "http://localhost:8848",
    string groupName = "DEFAULT_GROUP") : IAgentRegistry
{
    public async ValueTask RegisterAsync(AgentCard card, CancellationToken ct = default)
    {
        var ip = ParseHost(card.Endpoint);
        var port = ParsePort(card.Endpoint);
        var url = $"{serverAddr}/nacos/v1/ns/instance?" +
                  $"serviceName={card.Name}&" +
                  $"ip={ip}&port={port}&" +
                  $"groupName={groupName}&" +
                  $"metadata={Uri.EscapeDataString($"{{\"description\":\"{card.Description}\",\"provider\":\"{card.Provider ?? "agentscope-dotnet"}\"}}")}&" +
                  $"ephemeral=true";

        using var resp = await httpClient.PostAsync(url, null, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async ValueTask UnregisterAsync(string agentId, CancellationToken ct = default)
    {
        var url = $"{serverAddr}/nacos/v1/ns/instance?" +
                  $"serviceName={agentId}&groupName={groupName}";
        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        using var resp = await httpClient.SendAsync(req, ct);
    }

    public async ValueTask<AgentCard?> ResolveAsync(string agentId, CancellationToken ct = default)
    {
        var url = $"{serverAddr}/nacos/v1/ns/instance/list?" +
                  $"serviceName={agentId}&groupName={groupName}&healthyOnly=true";
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
            agentId,
            inst.TryGetProperty("serviceName", out var sn) ? sn.GetString() ?? agentId : agentId,
            meta.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
            $"{ip}:{port}");
    }

    public async IAsyncEnumerable<AgentCard> ListAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 对标 Java NacosA2aRegistry 列举：先取服务列表，再逐个解析实例
        var servicesUrl = $"{serverAddr}/nacos/v1/ns/service/list?pageNo=1&pageSize=1000&groupName={groupName}";
        using var servicesResp = await httpClient.GetAsync(servicesUrl, ct);
        if (!servicesResp.IsSuccessStatusCode) yield break;

        var servicesJson = await servicesResp.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (!servicesJson.TryGetProperty("doms", out var doms) || doms.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var dom in doms.EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();
            var serviceName = dom.GetString();
            if (string.IsNullOrEmpty(serviceName)) continue;

            var card = await ResolveAsync(serviceName, ct);
            if (card != null) yield return card;
        }
    }

    private static string ParseHost(string endpoint)
    {
        var parts = endpoint.Split(':');
        return parts.Length > 0 ? parts[0] : "localhost";
    }

    private static int ParsePort(string endpoint)
    {
        var parts = endpoint.Split(':');
        return parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 80;
    }
}
