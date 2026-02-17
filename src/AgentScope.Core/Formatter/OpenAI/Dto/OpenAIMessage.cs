// Copyright (c) 2024 AgentScope team.
// Licensed under the Apache License, Version 2.0.

using System.Text.Json.Serialization;

namespace AgentScope.Core.Formatter.OpenAI.Dto;

/// <summary>
/// OpenAI API 消息对象
/// OpenAI API message object
/// 
/// 对应 Java: io.agentscope.core.formatter.openai.dto.OpenAIMessage
/// </summary>
public record OpenAIMessage
{
    /// <summary>
    /// 消息角色：system, user, assistant, tool
    /// Message role: system, user, assistant, tool
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    /// <summary>
    /// 消息内容，可以是字符串或内容对象数组
    /// Message content, can be string or array of content objects
    /// </summary>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Content { get; init; }

    /// <summary>
    /// 消息名称（可选）
    /// Message name (optional)
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>
    /// 工具调用列表（assistant消息）
    /// Tool calls (for assistant messages)
    /// </summary>
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OpenAIToolCall>? ToolCalls { get; init; }

    /// <summary>
    /// 工具调用ID（tool消息）
    /// Tool call ID (for tool messages)
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; init; }
}

/// <summary>
/// 消息内容对象（用于多模态）
/// Message content object (for multimodal)
/// </summary>
public record OpenAIMessageContent
{
    /// <summary>
    /// 内容类型：text, image_url, video_url
    /// Content type: text, image_url, video_url
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// 文本内容（type=text时）
    /// Text content (when type=text)
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    /// <summary>
    /// 图片URL（type=image_url时）
    /// Image URL (when type=image_url)
    /// </summary>
    [JsonPropertyName("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIImageUrl? ImageUrl { get; init; }

    /// <summary>
    /// 视频URL（type=video_url时）
    /// Video URL (when type=video_url)
    /// </summary>
    [JsonPropertyName("video_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIVideoUrl? VideoUrl { get; init; }
}

/// <summary>
/// 图片URL对象
/// Image URL object
/// </summary>
public record OpenAIImageUrl
{
    /// <summary>
    /// 图片URL
    /// Image URL
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// 详细程度：low, high, auto
    /// Detail level: low, high, auto
    /// </summary>
    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }
}

/// <summary>
/// 视频URL对象
/// Video URL object
/// </summary>
public record OpenAIVideoUrl
{
    /// <summary>
    /// 视频URL
    /// Video URL
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }
}
