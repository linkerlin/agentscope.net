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
/// Remote confirm decision. Represents user's approve/deny response for a tool call.
/// 远程确认决策。表示用户对工具调用的批准或拒绝响应。
/// </summary>
public sealed class RemoteConfirmDecision
{
    /// <summary>The tool call identifier being decided / 待决策的工具调用标识</summary>
    public string? ToolCallId { get; set; }

    /// <summary>Whether the tool call is approved / 是否批准该工具调用</summary>
    public bool Approved { get; set; }

    /// <summary>
    /// Default constructor for deserialization.
    /// 反序列化用默认构造。
    /// </summary>
    public RemoteConfirmDecision() { }

    /// <summary>
    /// Initializes a confirm decision with tool call ID and approval status.
    /// 使用工具调用 ID 和批准状态初始化确认决策。
    /// </summary>
    /// <param name="toolCallId">Tool call identifier / 工具调用标识</param>
    /// <param name="approved">Approval status / 批准状态</param>
    public RemoteConfirmDecision(string toolCallId, bool approved)
    {
        ToolCallId = toolCallId;
        Approved = approved;
    }
}
