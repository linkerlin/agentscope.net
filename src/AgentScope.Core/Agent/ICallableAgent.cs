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
/// Defines the contract for agents that can be called/invoked with messages.
/// This is the fundamental communication interface in the AgentScope framework.
/// Counterpart to Java: io.agentscope.core.agent.CallableAgent.
/// Single-value return uses Task&lt;T&gt; (counterpart to Mono&lt;T&gt; in reactive programming).
/// 定义可被消息调用的 Agent 契约。
/// 这是 AgentScope 框架中最基本的通信接口。
/// 对应 Java: io.agentscope.core.agent.CallableAgent。
/// 单值返回使用 Task&lt;T&gt;（响应式编程中对应 Mono&lt;T&gt;）。
/// </summary>
public interface ICallableAgent
{
    /// <summary>
    /// Calls the agent with a list of messages and returns the result message.
    /// This is the primary invocation method for batch message processing.
    /// 使用消息列表调用 Agent，返回结果消息。
    /// 这是批量消息处理的主要调用方法。
    /// </summary>
    /// <param name="messages">The list of input messages / 输入消息列表</param>
    /// <param name="context">Optional runtime context for execution / 可选的运行时执行上下文</param>
    /// <returns>The result message produced by the agent / Agent 产生的结果消息</returns>
    Task<Msg> CallAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null);

    /// <summary>
    /// Calls the agent with a single message and returns the result message.
    /// Convenience overload for single-message scenarios.
    /// 使用单条消息调用 Agent，返回结果消息。
    /// 单条消息场景的便捷重载方法。
    /// </summary>
    /// <param name="message">The input message / 输入消息</param>
    /// <param name="context">Optional runtime context / 可选的运行时上下文</param>
    /// <returns>The result message / 结果消息</returns>
    Task<Msg> CallAsync(Msg message, RuntimeContext? context = null);

    /// <summary>
    /// Calls the agent with plain text, automatically wrapped as a user message.
    /// Convenience overload for simple text-based interactions.
    /// 使用纯文本调用 Agent，自动包装为用户消息。
    /// 简单文本交互场景的便捷重载方法。
    /// </summary>
    /// <param name="text">The input text / 输入文本</param>
    /// <param name="context">Optional runtime context / 可选的运行时上下文</param>
    /// <returns>The result message / 结果消息</returns>
    Task<Msg> CallAsync(string text, RuntimeContext? context = null);
}
