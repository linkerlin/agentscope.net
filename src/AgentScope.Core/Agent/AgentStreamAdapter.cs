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
/// 将任意 IAgent 桥接为事件流：在不修改原有 IAgent 的前提下，通过 CallAsync 得到结果后产出事件序列。
/// </summary>
public sealed class AgentStreamAdapter : IStreamableAgent
{
    private readonly IAgent _inner;

    public AgentStreamAdapter(IAgent inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public string Name => _inner.Name;

    public async IAsyncEnumerable<Event> StreamEventsAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
    {
        if (messages.Count == 0)
        {
            yield return new Event(EventType.ReasoningFinish, null, true);
            yield break;
        }

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

    public async IAsyncEnumerable<Event> StreamEventsAsync(Msg message, RuntimeContext? context = null)
    {
        await foreach (var ev in StreamEventsAsync(new[] { message }, context))
        {
            yield return ev;
        }
    }
}
