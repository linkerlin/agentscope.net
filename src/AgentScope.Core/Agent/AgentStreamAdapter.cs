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

using System;
using System.Collections.Generic;
using System.Linq;
using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Core.Agent;

/// <summary>
/// Bridges any IAgent to an event stream adapter without modifying the original IAgent.
/// After obtaining the result via CallAsync, it produces a sequence of events.
/// If the inner agent already supports IStreamableAgent, it delegates directly
/// to preserve the full Reasoning/Acting/Summary event sequence.
/// 将任意 IAgent 桥接为事件流适配器，无需修改原有的 IAgent。
/// 通过 CallAsync 得到结果后产出事件序列。
/// 如果内部 Agent 已支持 IStreamableAgent，则直接委派以保留完整的
/// Reasoning/Acting/Summary 事件序列。
/// </summary>
public sealed class AgentStreamAdapter : IStreamableAgent
{
    private readonly IAgent _inner;

    /// <summary>
    /// Initializes a new instance of the AgentStreamAdapter.
    /// 初始化 AgentStreamAdapter 的新实例。
    /// </summary>
    /// <param name="inner">The agent to adapt / 要适配的 Agent</param>
    public AgentStreamAdapter(IAgent inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>
    /// Gets the name of the inner agent.
    /// 获取内部 Agent 的名称。
    /// </summary>
    public string Name => _inner.Name;

    /// <summary>
    /// Streams events by processing a list of messages.
    /// If the inner agent is streamable, delegates directly; otherwise,
    /// calls CallAsync and wraps the result as a single ActingFinish event.
    /// 流式处理消息列表产出事件。
    /// 如果内部 Agent 支持流式，直接委派；否则调用 CallAsync 并将结果包装为单个 ActingFinish 事件。
    /// </summary>
    public async IAsyncEnumerable<Event> StreamEventsAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
    {
        if (messages.Count == 0)
        {
            yield return new Event(EventType.ReasoningFinish, null, true);
            yield break;
        }

        // If the inner agent itself supports streaming, delegate directly
        // to preserve its full Reasoning/Acting/Summary event sequence,
        // rather than degrading to a single ActingFinish event after CallAsync.
        // 内层代理本身可流式时，直接委派，保留其完整的 Reasoning/Acting/Summary 事件序列，
        // 而不是退化为「CallAsync 后补一个 ActingFinish」的单事件形态。
        if (!ReferenceEquals(_inner, this) && _inner is IStreamableAgent streamable)
        {
            await foreach (var ev in streamable.StreamEventsAsync(messages, context).ConfigureAwait(false))
            {
                yield return ev;
            }
            yield break;
        }

        var lastInput = messages[messages.Count - 1];
        Msg? response = null;
        string? errorMessage = null;
        try
        {
            response = await _inner.CallAsync(lastInput, context).ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            errorMessage = ex.Message;
        }
        if (errorMessage != null)
        {
            yield return Event.ErrorEvent(null, errorMessage, isLast: true);
            yield break;
        }
        yield return new Event(EventType.ActingFinish, response!, isLast: true);
    }

    /// <summary>
    /// Streams events by processing a single message. Delegates to the list overload.
    /// 流式处理单条消息产出事件。委托给列表重载方法。
    /// </summary>
    public async IAsyncEnumerable<Event> StreamEventsAsync(Msg message, RuntimeContext? context = null)
    {
        await foreach (var ev in StreamEventsAsync(new[] { message }, context))
        {
            yield return ev;
        }
    }
}
