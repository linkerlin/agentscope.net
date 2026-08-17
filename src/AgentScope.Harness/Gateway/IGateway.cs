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

using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 网关接口。对标 Java Gateway。
/// 负责 Agent 消息的路由、子 Agent 桥接、会话串行化。
/// </summary>
public interface IGateway
{
    /// <summary>
    /// 同步运行 Agent，返回单一响应消息。
    /// Run the agent synchronously and return a single response message.
    /// </summary>
    /// <param name="input">输入消息 / The input message.</param>
    /// <param name="context">运行时上下文，可选 / Optional runtime context.</param>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    /// <returns>Agent 响应消息 / The agent response message.</returns>
    Task<Msg> RunAsync(Msg input, RuntimeContext? context = null, CancellationToken ct = default);

    /// <summary>
    /// 流式运行 Agent，返回事件流。
    /// Run the agent in streaming mode, returning an event stream.
    /// </summary>
    /// <param name="input">输入消息 / The input message.</param>
    /// <param name="context">运行时上下文，可选 / Optional runtime context.</param>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    /// <returns>事件异步流 / The async event stream.</returns>
    IAsyncEnumerable<Event> RunStreamAsync(Msg input, RuntimeContext? context = null, CancellationToken ct = default);
}
