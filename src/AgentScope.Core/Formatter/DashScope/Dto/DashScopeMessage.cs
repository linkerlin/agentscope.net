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
/// DashScope message DTO.
/// DashScope 消息 DTO
/// 
/// Java参考: io.agentscope.core.formatter.dashscope.dto.DashScopeMessage
/// </summary>
public class DashScopeMessage
{
    /// <summary>
    /// Message role: "system", "user", "assistant", or "tool"
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    /// <summary>
    /// Message content - can be String for text-only, or List&lt;DashScopeContentPart&gt; for multimodal
    /// </summary>
    [JsonPropertyName("content")]
    public required object Content { get; set; }

    /// <summary>
    /// Tool name (for role="tool")
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>
    /// Tool call ID (for role="tool")
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }

    /// <summary>
    /// Tool calls made by assistant
    /// </summary>
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DashScopeToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// Reasoning/thinking content (for assistant messages with thinking enabled)
    /// </summary>
    [JsonPropertyName("reasoning_content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningContent { get; set; }

    /// <summary>
    /// Get content as string (for text-only messages)
    /// </summary>
    [JsonIgnore]
    public string? ContentAsString => Content as string;

    /// <summary>
    /// Get content as list (for multimodal messages)
    /// </summary>
    [JsonIgnore]
    public List<DashScopeContentPart>? ContentAsList => Content as List<DashScopeContentPart>;

    /// <summary>
    /// Check if this message has multimodal content
    /// </summary>
    [JsonIgnore]
    public bool IsMultimodal => Content is List<DashScopeContentPart>;
}
