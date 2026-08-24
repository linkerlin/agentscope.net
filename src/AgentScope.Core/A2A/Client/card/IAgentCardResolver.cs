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
/// AgentCard resolver interface. Corresponds to Java AgentCardResolver.
/// AgentCard 解析器接口。对标 Java AgentCardResolver。
/// </summary>
public interface IAgentCardResolver
{
    /// <summary>
    /// Resolves the AgentCard for the given agent name.
    /// 根据 agent 名称解析对应的 AgentCard
    /// </summary>
    /// <param name="agentName">The agent name or URL / agent 名称或 URL</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>The resolved AgentCard, or null if not found / 解析结果，找不到时返回 null</returns>
    Task<AgentCard?> ResolveAsync(string agentName, CancellationToken ct = default);
}
