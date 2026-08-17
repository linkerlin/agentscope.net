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

namespace AgentScope.Core.Service.Discovery;

/// <summary>
/// Agent registration interface (A2A SPI). Corresponds to Java AgentRegistry.
/// Agent 注册接口（A2A SPI）。对标 Java AgentRegistry。
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Registers an agent card in the registry.
    /// 在注册表中注册一个 Agent 卡片。
    /// </summary>
    ValueTask RegisterAsync(AgentCard card, CancellationToken ct = default);

    /// <summary>
    /// Unregisters an agent by its unique identifier.
    /// 根据唯一标识符注销一个 Agent。
    /// </summary>
    ValueTask UnregisterAsync(string agentId, CancellationToken ct = default);

    /// <summary>
    /// Resolves an agent card by its unique identifier.
    /// 根据唯一标识符解析 Agent 卡片。
    /// </summary>
    ValueTask<AgentCard?> ResolveAsync(string agentId, CancellationToken ct = default);

    /// <summary>
    /// Lists all registered agent cards as an async stream.
    /// 以异步流形式列出所有已注册的 Agent 卡片。
    /// </summary>
    IAsyncEnumerable<AgentCard> ListAsync(CancellationToken ct = default);
}

/// <summary>
/// Agent capability description card containing identity, endpoint, and skill information.
/// Agent 能力描述卡片，包含身份、端点和技能信息。
/// Corresponds to Java: AgentCard
/// </summary>
public sealed record AgentCard(
    string AgentId,
    string Name,
    string Description,
    string Endpoint,
    string? Provider = null,
    IReadOnlyList<string>? Skills = null);
