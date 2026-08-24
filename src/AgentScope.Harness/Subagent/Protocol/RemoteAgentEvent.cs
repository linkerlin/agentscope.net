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

using System.Text.Json.Serialization;

namespace AgentScope.Harness.Subagent.Protocol;

/// <summary>
/// Remote agent event DTO. Carries streaming event data from a remote agent execution.
/// 远程子代理事件 DTO。携带远程 Agent 执行过程中的流式事件数据。
/// </summary>
public sealed class RemoteAgentEvent
{
    /// <summary>Event sequence number / 事件序列号</summary>
    public long Seq { get; set; }

    /// <summary>Event type / 事件类型</summary>
    public RemoteEventType Type { get; set; }

    /// <summary>Task identifier / 任务标识</summary>
    public string? TaskId { get; set; }

    /// <summary>Agent identifier / Agent 标识</summary>
    public string? AgentId { get; set; }

    /// <summary>ISO 8601 timestamp / ISO 8601 时间戳</summary>
    public string? Timestamp { get; set; }

    /// <summary>Text content delta / 文本内容增量</summary>
    public string? Text { get; set; }

    /// <summary>Tool call identifier / 工具调用标识</summary>
    public string? ToolCallId { get; set; }

    /// <summary>Tool name / 工具名称</summary>
    public string? ToolName { get; set; }

    /// <summary>Tool input JSON / 工具输入 JSON</summary>
    public string? ToolInput { get; set; }

    /// <summary>Execution status string / 执行状态字符串</summary>
    public string? Status { get; set; }

    /// <summary>Error message if any / 错误信息</summary>
    public string? Error { get; set; }

    /// <summary>Custom event type discriminator / 自定义事件类型标识</summary>
    public string? EventType { get; set; }

    /// <summary>Custom event payload / 自定义事件负载</summary>
    public string? Payload { get; set; }

    /// <summary>Pending confirmations requiring user approval / 待用户确认的确认项</summary>
    public List<RemotePendingConfirm>? PendingConfirms { get; set; }
}
