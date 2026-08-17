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

using AgentScope.Core.Service.Discovery;

namespace AgentScope.Core.A2A.Client.Card;

/// <summary>
/// Fixed AgentCard resolver. Corresponds to Java FixedAgentCardResolver.
/// Returns the same card for all agent names.
/// 固定 AgentCard 解析器。对标 Java FixedAgentCardResolver。
/// 所有名称返回同一张卡片。
/// </summary>
public sealed class FixedAgentCardResolver(AgentCard card) : IAgentCardResolver
{
    /// <inheritdoc />
    /// <remarks>
    /// Always returns the pre-configured card regardless of agentName.
    /// 无论 agentName 为何值，始终返回预配置的卡片。
    /// </remarks>
    public Task<AgentCard?> ResolveAsync(string agentName, CancellationToken ct = default) =>
        Task.FromResult<AgentCard?>(card);
}
