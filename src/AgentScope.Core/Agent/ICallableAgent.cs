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
/// Callable Agent interface. Counterpart to Java CallableAgent.
/// Single-value return uses Task&lt;T&gt; (counterpart to Mono&lt;T&gt;).
/// 可调用的 Agent 接口，对应 Java CallableAgent。
/// 单值返回使用 Task&lt;T&gt;（对应 Mono&lt;T&gt;）。
/// </summary>
public interface ICallableAgent
{
    /// <summary>
    /// Calls the agent with a list of messages and returns the result message.
    /// 使用消息列表调用 Agent，返回结果消息。
    /// </summary>
    Task<Msg> CallAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null);

    /// <summary>
    /// Calls the agent with a single message and returns the result message.
    /// 使用单条消息调用 Agent，返回结果消息。
    /// </summary>
    Task<Msg> CallAsync(Msg message, RuntimeContext? context = null);

    /// <summary>
    /// Calls the agent with plain text, automatically wrapped as a UserMessage.
    /// 使用文本调用 Agent，自动包装为 UserMessage。
    /// </summary>
    Task<Msg> CallAsync(string text, RuntimeContext? context = null);
}
