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
/// Anthropic 内容块基类
/// Anthropic content block base class
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextBlock), typeDiscriminator: "text")]
[JsonDerivedType(typeof(ImageBlock), typeDiscriminator: "image")]
[JsonDerivedType(typeof(ToolUseBlock), typeDiscriminator: "tool_use")]
[JsonDerivedType(typeof(ToolResultBlock), typeDiscriminator: "tool_result")]
[JsonDerivedType(typeof(ThinkingBlock), typeDiscriminator: "thinking")]
public abstract record AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

/// <summary>
/// 文本内容块
/// Text content block
/// </summary>
public record TextBlock : AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public override string Type => "text";

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

/// <summary>
/// 图片来源
/// Image source
/// </summary>
public record ImageSource
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }  // "base64"

    [JsonPropertyName("media_type")]
    public required string MediaType { get; init; }  // "image/png", "image/jpeg", etc.

    [JsonPropertyName("data")]
    public required string Data { get; init; }  // base64 string
}

/// <summary>
/// 图片内容块
/// Image content block
/// </summary>
public record ImageBlock : AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public override string Type => "image";

    [JsonPropertyName("source")]
    public required ImageSource Source { get; init; }
}

/// <summary>
/// 工具使用块
/// Tool use block (represents a tool call from the assistant)
/// </summary>
public record ToolUseBlock : AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public override string Type => "tool_use";

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("input")]
    public required Dictionary<string, object> Input { get; init; }
}

/// <summary>
/// 工具结果内容块
/// Tool result content block
/// </summary>
public record ToolResultContent
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }  // "text" or "image"

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("source")]
    public ImageSource? Source { get; init; }
}

/// <summary>
/// 工具结果块
/// Tool result block (represents the result of a tool execution)
/// </summary>
public record ToolResultBlock : AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public override string Type => "tool_result";

    [JsonPropertyName("tool_use_id")]
    public required string ToolUseId { get; init; }

    [JsonPropertyName("content")]
    public required List<ToolResultContent> Content { get; init; }

    [JsonPropertyName("is_error")]
    public bool? IsError { get; init; }
}

/// <summary>
/// 思考内容块（Claude 3.7 Sonnet 特有的 extended thinking 功能）
/// Thinking block (Claude 3.7 Sonnet extended thinking feature)
/// </summary>
public record ThinkingBlock : AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public override string Type => "thinking";

    [JsonPropertyName("thinking")]
    public required string Thinking { get; init; }

    [JsonPropertyName("signature")]
    public string? Signature { get; init; }
}

/// <summary>
/// Redacted thinking block (for sensitive content)
/// </summary>
public record RedactedThinkingBlock : AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public override string Type => "redacted_thinking";

    [JsonPropertyName("data")]
    public required string Data { get; init; }
}
