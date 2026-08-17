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

using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AgentScope.Core.Service.Discovery;

namespace AgentScope.Extensions.Nacos;

/// <summary>
/// Nacos Agent registry implemented via the Nacos HTTP API.
/// Corresponds to the Java NacosAgentRegistry / NacosA2aRegistry.
/// Uses the Nacos Open API directly to avoid depending on a specific version of nacos-sdk-csharp.
/// Nacos Agent 注册表（HTTP API 实现）。对标 Java NacosAgentRegistry / NacosA2aRegistry。
/// 通过 Nacos Open API 注册/发现 Agent，不依赖 nacos-sdk-csharp 的具体版本。
/// </summary>
/// <param name="httpClient">The HttpClient used for Nacos API calls / 用于调用 Nacos API 的 HttpClient</param>
/// <param name="serverAddr">Nacos server address / Nacos 服务器地址</param>
/// <param name="groupName">Nacos group name / Nacos 分组名称</param>
public sealed class NacosAgentRegistry(
    HttpClient httpClient,
    string serverAddr = "http://localhost:8848",
    string groupName = "DEFAULT_GROUP") : IAgentRegistry
{
    /// <summary>
    /// Registers an Agent as an ephemeral Nacos instance.
    /// 将 Agent 注册为 Nacos 临时实例。
    /// </summary>
    /// <param name="card">The agent card to register / 要注册的 Agent 卡片</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
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

    /// <summary>
    /// Unregisters an Agent from Nacos by deleting its instance record.
    /// 从 Nacos 注销 Agent，删除其实例记录。
    /// </summary>
    /// <param name="agentId">The agent/service ID to unregister / 要注销的 Agent/服务 ID</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async ValueTask UnregisterAsync(string agentId, CancellationToken ct = default)
    {
        var url = $"{serverAddr}/nacos/v1/ns/instance?" +
                  $"serviceName={agentId}&groupName={groupName}";
        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        using var resp = await httpClient.SendAsync(req, ct);
    }

    /// <summary>
    /// Resolves an AgentCard by querying Nacos for healthy instances of the given service.
    /// 通过查询 Nacos 获取指定服务的健康实例来解析 AgentCard。
    /// </summary>
    /// <param name="agentId">The agent/service ID to resolve / 要解析的 Agent/服务 ID</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>The resolved AgentCard, or null if not found / 解析到的 AgentCard，未找到时返回 null</returns>
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

    /// <summary>
    /// Lists all registered agents by first fetching the service list and then resolving each service.
    /// 先获取服务列表，再逐个解析每个服务，列举所有已注册的 Agent。
    /// </summary>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>An async-enumerable sequence of AgentCards / AgentCard 的异步可枚举序列</returns>
    public async IAsyncEnumerable<AgentCard> ListAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Corresponds to Java NacosA2aRegistry listing: first get service list, then resolve each one
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

    /// <summary>
    /// Extracts the host part from an endpoint string (e.g. "192.168.1.1:8080" -> "192.168.1.1").
    /// 从端点字符串中提取主机部分（如 "192.168.1.1:8080" -> "192.168.1.1"）。
    /// </summary>
    private static string ParseHost(string endpoint)
    {
        var parts = endpoint.Split(':');
        return parts.Length > 0 ? parts[0] : "localhost";
    }

    /// <summary>
    /// Extracts the port part from an endpoint string (e.g. "192.168.1.1:8080" -> 8080). Defaults to 80.
    /// 从端点字符串中提取端口部分（如 "192.168.1.1:8080" -> 8080），默认返回 80。
    /// </summary>
    private static int ParsePort(string endpoint)
    {
        var parts = endpoint.Split(':');
        return parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 80;
    }
}
