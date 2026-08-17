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
using System.Text.Json;
using AgentScope.Core.Service.Discovery;

namespace AgentScope.Extensions.Nacos;

/// <summary>
/// Nacos Agent Card resolver implemented via the Nacos HTTP API.
/// Corresponds to the Java NacosAgentCardResolver.
/// Nacos Agent Card 解析器（HTTP API 实现）。对标 Java NacosAgentCardResolver。
/// </summary>
/// <param name="httpClient">The HttpClient used for Nacos API calls / 用于调用 Nacos API 的 HttpClient</param>
/// <param name="serverAddr">Nacos server address / Nacos 服务器地址</param>
/// <param name="groupName">Nacos group name / Nacos 分组名称</param>
public sealed class NacosAgentCardResolver(
    HttpClient httpClient,
    string serverAddr = "http://localhost:8848",
    string groupName = "DEFAULT_GROUP")
{
    /// <summary>
    /// Resolves an AgentCard by querying Nacos for healthy instances of the given service.
    /// 通过查询 Nacos 获取指定服务的健康实例来解析 AgentCard。
    /// </summary>
    /// <param name="agentName">The agent/service name to resolve / 要解析的 Agent/服务名称</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>The resolved AgentCard, or null if no healthy instance is found / 解析到的 AgentCard，未找到健康实例时返回 null</returns>
    public async Task<AgentCard?> ResolveAsync(string agentName, CancellationToken ct = default)
    {
        // Query Nacos for healthy instances of the specified service
        // 查询 Nacos 获取指定服务的健康实例列表
        var url = $"{serverAddr}/nacos/v1/ns/instance/list?" +
                  $"serviceName={agentName}&groupName={groupName}&healthyOnly=true";
        using var resp = await httpClient.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (!json.TryGetProperty("hosts", out var hosts) || hosts.GetArrayLength() == 0)
            return null;

        // Take the first healthy instance and extract its metadata
        // 取第一个健康实例并提取元数据
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
