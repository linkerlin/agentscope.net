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

using AgentScope.Core.Events;

namespace AgentScope.Harness.Subagent.Protocol;

/// <summary>
/// Remote event codec. Encodes core AgentEvent into RemoteAgentEvent for transport.
/// 远程事件编解码器。将核心 AgentEvent 编码为可传输的 RemoteAgentEvent。
/// </summary>
public static class RemoteEventCodec
{
    /// <summary>
    /// Converts a core Agent Event to a RemoteAgentEvent.
    /// 将核心 Agent 事件转换为 RemoteAgentEvent。
    /// </summary>
    /// <param name="agentEvent">The source agent event / 源 Agent 事件</param>
    /// <returns>The encoded remote event, or null if conversion fails / 编码后的远程事件，失败时返回 null</returns>
    public static RemoteAgentEvent? FromAgentEvent(Event agentEvent)
    {
        var type = agentEvent.Type switch
        {
            EventType.ReasoningStart => RemoteEventType.RunStarted,
            EventType.ReasoningFinish => RemoteEventType.RunFinished,
            EventType.ToolCallStart => RemoteEventType.ToolCallStart,
            EventType.ToolCallFinish => RemoteEventType.ToolCallEnd,
            _ => RemoteEventType.AgentEvent
        };

        return new RemoteAgentEvent
        {
            Type = type,
            Text = agentEvent.Message?.GetTextContent(),
            Status = agentEvent.IsLast ? "completed" : "running",
            Timestamp = DateTime.UtcNow.ToString("O")
        };
    }
}
