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

using System.Threading.Tasks;
using AgentScope.Core.Message;

namespace AgentScope.Core.Memory;

/// <summary>
/// A hook that automatically stores assistant messages into LongTermMemory
/// after each agent response, enabling persistent memory without explicit
/// agent action.
///
/// 一个 Hook，在每次 Agent 响应后自动将 Assistant 消息存入 LongTermMemory，
/// 实现无需 Agent 显式操作的持久化记忆。
/// Corresponds to Java: io.agentscope.core.memory.StaticLongTermMemoryHook
/// </summary>
public class StaticLongTermMemoryHook
{
    /// <summary>
    /// The ILongTermMemory instance to write into.
    /// 要写入的 ILongTermMemory 实例。
    /// </summary>
    private readonly ILongTermMemory _longTermMemory;

    /// <summary>
    /// Initializes a new instance of StaticLongTermMemoryHook.
    /// 初始化 StaticLongTermMemoryHook 的新实例。
    /// </summary>
    /// <param name="longTermMemory">The long-term memory instance. / 长期记忆实例。</param>
    public StaticLongTermMemoryHook(ILongTermMemory longTermMemory)
    {
        _longTermMemory = longTermMemory ?? throw new System.ArgumentNullException(nameof(longTermMemory));
    }

    /// <summary>
    /// Called after an agent response is received, to store assistant messages
    /// into long-term memory automatically.
    /// 在收到 Agent 响应后调用，自动将 Assistant 消息存入长期记忆。
    /// </summary>
    /// <param name="response">The agent response message. / Agent 响应消息。</param>
    public async Task OnAfterResponseAsync(Msg response)
    {
        // Only store assistant messages to avoid duplicating user input.
        // 只存储 Assistant 消息，避免重复存储用户输入。
        // Note: Msg.Role is a string, not MsgRole enum.
        // 注意：Msg.Role 是 string 类型，不是 MsgRole 枚举。
        if (string.Equals(response.Role, "assistant", System.StringComparison.OrdinalIgnoreCase))
        {
            // Use the message content as the memory entry, tagged with "auto".
            // 以消息内容作为记忆条目，标记为 "auto" 标签。
            var metadata = new System.Collections.Generic.Dictionary<string, object>
            {
                { "tags", "auto" }
            };
            await _longTermMemory.AddAsync(response.GetTextContent() ?? string.Empty, metadata);
        }
    }

    /// <summary>
    /// Called before an agent request is sent. No pre-request logic needed
    /// for this hook.
    /// 在发送 Agent 请求前调用。此 Hook 不需要请求前逻辑。
    /// </summary>
    /// <param name="request">The outgoing request message. / 发出的请求消息。</param>
    public async Task OnBeforeRequestAsync(Msg request)
    {
        // No pre-request logic needed for this hook.
        // 此 Hook 不需要请求前逻辑。
        await Task.CompletedTask;
    }
}
