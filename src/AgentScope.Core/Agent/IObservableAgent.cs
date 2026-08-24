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

using System.Collections.Generic;
using System.Threading.Tasks;
using AgentScope.Core.Message;

namespace AgentScope.Core.Agent;

/// <summary>
/// Defines the contract for agents that can observe and react to messages from other agents.
/// This enables passive message reception in multi-agent systems, where agents
/// can monitor and respond to communications without being explicitly called.
/// Counterpart to Java: io.agentscope.core.agent.ObservableAgent.
/// 定义可观察 Agent 的契约，允许 Agent 观察并响应来自其他 Agent 的消息。
/// 这实现了多 Agent 系统中的被动消息接收，Agent 可以监控和响应通信而无需被显式调用。
/// 对应 Java: io.agentscope.core.agent.ObservableAgent。
/// </summary>
public interface IObservableAgent
{
    /// <summary>
    /// Observes and processes a single message from another agent.
    /// The agent may react to the message based on its internal logic.
    /// 观察并处理来自其他 Agent 的单条消息。
    /// Agent 可以根据其内部逻辑对消息做出反应。
    /// </summary>
    /// <param name="message">The message to observe / 要观察的消息</param>
    /// <param name="context">Optional runtime context / 可选的运行时上下文</param>
    Task ObserveAsync(Msg message, RuntimeContext? context = null);

    /// <summary>
    /// Observes and processes multiple messages from other agents.
    /// 观察并处理来自其他 Agent 的多条消息。
    /// </summary>
    /// <param name="messages">The messages to observe / 要观察的消息列表</param>
    /// <param name="context">Optional runtime context / 可选的运行时上下文</param>
    Task ObserveAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null);
}
