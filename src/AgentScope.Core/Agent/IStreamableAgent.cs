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
using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Core.Agent;

/// <summary>
/// Defines the contract for agents that support streaming output of events.
/// Streaming enables real-time, incremental processing where events are yielded
/// one by one as they are produced, rather than waiting for the complete result.
/// Counterpart to Java: io.agentscope.core.agent.StreamableAgent.
/// Streaming uses IAsyncEnumerable&lt;Event&gt; (counterpart to Flux&lt;AgentEvent&gt; in reactive programming).
/// 定义支持流式事件输出的 Agent 契约。
/// 流式输出支持实时、增量处理，事件在产生时逐个产出，无需等待完整结果。
/// 对应 Java: io.agentscope.core.agent.StreamableAgent。
/// 流式返回使用 IAsyncEnumerable&lt;Event&gt;（响应式编程中对应 Flux&lt;AgentEvent&gt;）。
/// </summary>
public interface IStreamableAgent
{
    /// <summary>
    /// Processes a list of messages in streaming mode, yielding events one by one
    /// as they become available during processing.
    /// 流式处理消息列表，在处理过程中逐个产出事件。
    /// </summary>
    /// <param name="messages">The list of input messages / 输入消息列表</param>
    /// <param name="context">Optional runtime context / 可选的运行时上下文</param>
    /// <returns>An async enumerable of events produced during processing / 处理过程中产生的事件异步枚举</returns>
    IAsyncEnumerable<Event> StreamEventsAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null);

    /// <summary>
    /// Processes a single message in streaming mode, yielding events one by one.
    /// 流式处理单条消息，逐个产出事件。
    /// </summary>
    /// <param name="message">The input message / 输入消息</param>
    /// <param name="context">Optional runtime context / 可选的运行时上下文</param>
    /// <returns>An async enumerable of events / 事件异步枚举</returns>
    IAsyncEnumerable<Event> StreamEventsAsync(Msg message, RuntimeContext? context = null);
}
