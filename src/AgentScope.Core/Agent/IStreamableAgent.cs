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
/// Streamable Agent interface. Counterpart to Java StreamableAgent.
/// Streaming uses IAsyncEnumerable&lt;Event&gt; (counterpart to Flux&lt;AgentEvent&gt;).
/// 可流式输出的 Agent 接口，对应 Java StreamableAgent。
/// 流式返回使用 IAsyncEnumerable&lt;AgentEvent&gt;（对应 Flux&lt;AgentEvent&gt;）。
/// </summary>
public interface IStreamableAgent
{
    /// <summary>
    /// Processes a list of messages in streaming mode, yielding events one by one.
    /// 流式处理消息列表，逐个产出 AgentEvent。
    /// </summary>
    IAsyncEnumerable<Event> StreamEventsAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null);

    /// <summary>
    /// Processes a single message in streaming mode.
    /// 流式处理单条消息。
    /// </summary>
    IAsyncEnumerable<Event> StreamEventsAsync(Msg message, RuntimeContext? context = null);
}
