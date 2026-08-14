// Copyright 2024-2026 the original author or authors.
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

using AgentScope.Core.Agent;
using AgentScope.Harness.Gateway.Channel;

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 渠道运行时上下文解析器：从入站消息解析/构造 RuntimeContext（用户、会话、来源）。
/// 对应 Java: io.agentscope.harness.agent.gateway.channel.ChannelRuntimeContextResolver
/// </summary>
public sealed class ChannelRuntimeContextResolver
{
    /// <summary>根据入站消息构造 RuntimeContext。</summary>
    public RuntimeContext Resolve(InboundMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        var userId = message.AccountId ?? message.SenderId ?? "unknown";
        var sessionId = SessionIdUtils.Compose(userId, message.ChannelId ?? "", "");

        return new RuntimeContext(userId, sessionId);
    }
}
