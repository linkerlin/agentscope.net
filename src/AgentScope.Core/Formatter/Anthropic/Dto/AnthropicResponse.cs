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

namespace AgentScope.Core.Formatter.Anthropic.Dto;

/// <summary>
/// Anthropic Messages API 响应
/// Anthropic Messages API response
/// 
/// Java参考: com.anthropic.models.messages.Message
/// </summary>
public record AnthropicResponse
{
    /// <summary>
    /// 响应 ID
    /// Response ID
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// 类型（通常为 "message"）
    /// Type (usually "message")
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// 角色（"assistant"）
    /// Role ("assistant")
    /// </summary>
    [JsonPropertyName("role")]
    public required AnthropicRole Role { get; init; }

    /// <summary>
    /// 内容块列表
    /// List of content blocks
    /// </summary>
    [JsonPropertyName("content")]
    public required List<AnthropicContentBlock> Content { get; init; }

    /// <summary>
    /// 模型名称
    /// Model name
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// 停止原因
    /// Stop reason: "end_turn", "max_tokens", "stop_sequence", "tool_use"
    /// </summary>
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; init; }

    /// <summary>
    /// 停止序列
    /// Stop sequence that caused the stop
    /// </summary>
    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; init; }

    /// <summary>
    /// Token 使用量
    /// Token usage information
    /// </summary>
    [JsonPropertyName("usage")]
    public required AnthropicUsage Usage { get; init; }
}

/// <summary>
/// Token 使用量
/// Token usage
/// </summary>
public record AnthropicUsage
{
    /// <summary>
    /// 输入 token 数
    /// Input tokens
    /// </summary>
    [JsonPropertyName("input_tokens")]
    public required int InputTokens { get; init; }

    /// <summary>
    /// 输出 token 数
    /// Output tokens
    /// </summary>
    [JsonPropertyName("output_tokens")]
    public required int OutputTokens { get; init; }

    /// <summary>
    /// 缓存创建 token 数
    /// Cache creation input tokens
    /// </summary>
    [JsonPropertyName("cache_creation_input_tokens")]
    public int? CacheCreationInputTokens { get; init; }

    /// <summary>
    /// 缓存读取 token 数
    /// Cache read input tokens
    /// </summary>
    [JsonPropertyName("cache_read_input_tokens")]
    public int? CacheReadInputTokens { get; init; }
}

/// <summary>
/// 流式响应事件（用于 SSE 流）
/// Streaming response event (for SSE stream)
/// </summary>
public record AnthropicStreamEvent
{
    [JsonPropertyName("type")]
    public required string EventType { get; init; }

    [JsonPropertyName("message")]
    public AnthropicResponse? Message { get; init; }

    [JsonPropertyName("content_block")]
    public AnthropicContentBlock? ContentBlock { get; init; }

    [JsonPropertyName("delta")]
    public AnthropicContentDelta? Delta { get; init; }

    [JsonPropertyName("usage")]
    public AnthropicUsage? Usage { get; init; }

    [JsonPropertyName("index")]
    public int? Index { get; init; }
}

/// <summary>
/// 内容增量（流式响应）
/// Content delta for streaming responses
/// </summary>
public record AnthropicContentDelta
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("thinking")]
    public string? Thinking { get; init; }

    [JsonPropertyName("partial_json")]
    public string? PartialJson { get; init; }
}
