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
using System.Text.Json.Serialization;

namespace AgentScope.Core.Formatter.DashScope.Dto;

/// <summary>
/// DashScope message DTO, representing a single message in the conversation.
/// DashScope 消息 DTO，表示对话中的单条消息。
///
/// 支持系统消息、用户消息、助手消息和工具消息四种角色。
/// Supports four roles: system, user, assistant, and tool messages.
///
/// Java参考: io.agentscope.core.formatter.dashscope.dto.DashScopeMessage
/// </summary>
public class DashScopeMessage
{
    /// <summary>
    /// 消息角色："system", "user", "assistant" 或 "tool"
    /// Message role: "system", "user", "assistant", or "tool"
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    /// <summary>
    /// 消息内容。纯文本时为 string，多模态时为 List&lt;DashScopeContentPart&gt;。
    /// Message content. String for text-only, List&lt;DashScopeContentPart&gt; for multimodal.
    /// </summary>
    [JsonPropertyName("content")]
    public required object Content { get; set; }

    /// <summary>
    /// 工具名称，当 role="tool" 时指定调用的是哪个工具。
    /// Tool name, specifying which tool was called when role="tool".
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>
    /// 工具调用 ID，当 role="tool" 时关联到对应的工具调用。
    /// Tool call ID, linking back to the corresponding tool call when role="tool".
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }

    /// <summary>
    /// 助手消息中的工具调用列表，表示模型请求调用的函数。
    /// Tool calls made by the assistant, indicating functions the model wants to invoke.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DashScopeToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// 推理内容，当启用思考模式时包含模型的中文推理过程。
    /// Reasoning/thinking content, containing the model's reasoning process when thinking is enabled.
    /// </summary>
    [JsonPropertyName("reasoning_content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningContent { get; set; }

    /// <summary>
    /// 获取 string 类型的内容（纯文本消息）。
    /// Get content as string for text-only messages.
    /// </summary>
    [JsonIgnore]
    public string? ContentAsString => Content as string;

    /// <summary>
    /// 获取 List&lt;DashScopeContentPart&gt; 类型的内容（多模态消息）。
    /// Get content as list for multimodal messages.
    /// </summary>
    [JsonIgnore]
    public List<DashScopeContentPart>? ContentAsList => Content as List<DashScopeContentPart>;

    /// <summary>
    /// 检查消息是否为多模态格式。
    /// Check if this message has multimodal content.
    /// </summary>
    [JsonIgnore]
    public bool IsMultimodal => Content is List<DashScopeContentPart>;
}
