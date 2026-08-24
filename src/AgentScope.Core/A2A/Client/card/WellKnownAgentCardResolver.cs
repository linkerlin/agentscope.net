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
using AgentScope.Core.Service.Discovery;

namespace AgentScope.Core.A2A.Client.Card;

/// <summary>
/// Well-Known AgentCard resolver. Corresponds to Java WellKnownAgentCardResolver.
/// Fetches the card from /.well-known/agent-card.json.
/// Well-Known AgentCard 解析器。对标 Java WellKnownAgentCardResolver。
/// 从 /.well-known/agent-card.json 获取卡片。
/// </summary>
public sealed class WellKnownAgentCardResolver(HttpClient http) : IAgentCardResolver
{
    /// <inheritdoc />
    /// <remarks>
    /// Constructs URL as "https://{agentName}/.well-known/agent-card.json".
    /// If agentName already starts with "http", uses it as-is.
    /// 构造 URL 为 "https://{agentName}/.well-known/agent-card.json"。
    /// 如果 agentName 以 "http" 开头，则直接使用。
    /// </remarks>
    public async Task<AgentCard?> ResolveAsync(string agentName, CancellationToken ct = default)
    {
        // Determine base URL: use as-is if already has scheme, otherwise prepend https://
        // 确定基础 URL：如果已有 scheme 则直接使用，否则添加 https:// 前缀
        var baseUrl = agentName.StartsWith("http") ? agentName : $"https://{agentName}";
        var uri = $"{baseUrl.TrimEnd('/')}/.well-known/agent-card.json";
        try
        {
            return await http.GetFromJsonAsync<AgentCard>(uri, ct);
        }
        catch
        {
            // Silently return null on any fetch/parse error
            // 任何获取或解析错误时静默返回 null
            return null;
        }
    }
}
