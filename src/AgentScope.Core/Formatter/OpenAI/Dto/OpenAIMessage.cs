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

// Copyright (c) 2024 AgentScope team.
// Licensed under the Apache License, Version 2.0.

using System.Text.Json.Serialization;

namespace AgentScope.Core.Formatter.OpenAI.Dto;

/// <summary>
/// OpenAI API 消息对象<br />
/// OpenAI API message object<br />
/// 对应 Java: io.agentscope.core.formatter.openai.dto.OpenAIMessage
/// </summary>
public record OpenAIMessage
{
    /// <summary>
    /// 消息角色：system, user, assistant, tool<br />
    /// Message role: system, user, assistant, tool
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    /// <summary>
    /// 消息内容，可以是字符串或内容对象数组<br />
    /// Message content, can be string or array of content objects
    /// </summary>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Content { get; init; }

    /// <summary>
    /// 内容部分数组，用于多模态消息。<br />
    /// 设置此属性时，Content 会被自动设置为此数组。<br />
    /// Content parts array for multimodal messages.<br />
    /// When this is set, Content will be automatically set to this array.
    /// </summary>
    [JsonIgnore]
    public List<OpenAIMessageContent>? ContentParts
    {
        get => Content as List<OpenAIMessageContent>;
        init => Content = value;
    }

    /// <summary>
    /// 消息名称，用于区分同一角色的多个消息（可选）<br />
    /// Message name for distinguishing multiple messages of the same role (optional)
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>
    /// 工具调用列表，仅 assistant 消息包含<br />
    /// Tool calls list, only present in assistant messages
    /// </summary>
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OpenAIToolCall>? ToolCalls { get; set; }

    /// <summary>
    /// 工具调用响应 ID，仅 tool 消息包含<br />
    /// Tool call response ID, only present in tool messages
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; init; }

    /// <summary>
    /// 推理模型的思考内容（如 qwen-plus, deepseek-r1 等）<br />
    /// Reasoning/thinking content for reasoning models (e.g. qwen-plus, deepseek-r1, etc.)
    /// </summary>
    [JsonPropertyName("reasoning_content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningContent { get; init; }
}

/// <summary>
/// 多模态消息内容对象<br />
/// Multimodal message content object
/// </summary>
public record OpenAIMessageContent
{
    /// <summary>
    /// 内容类型：text, image_url, video_url, input_audio<br />
    /// Content type: text, image_url, video_url, input_audio
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// 文本内容（type=text 时）<br />
    /// Text content (when type=text)
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; init; }

    /// <summary>
    /// 图片URL（type=image_url 时）<br />
    /// Image URL (when type=image_url)
    /// </summary>
    [JsonPropertyName("image_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIImageUrl? ImageUrl { get; init; }

    /// <summary>
    /// 视频URL（type=video_url 时）<br />
    /// Video URL (when type=video_url)
    /// </summary>
    [JsonPropertyName("video_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIVideoUrl? VideoUrl { get; init; }

    /// <summary>
    /// 输入音频（type=input_audio 时）<br />
    /// Input audio (when type=input_audio)
    /// </summary>
    [JsonPropertyName("input_audio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIInputAudio? InputAudio { get; init; }
}

/// <summary>
/// 图片URL对象<br />
/// Image URL object
/// </summary>
public record OpenAIImageUrl
{
    /// <summary>
    /// 图片URL或data URI<br />
    /// Image URL or data URI
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// 详细程度：low, high, auto（可选）<br />
    /// Detail level: low, high, auto (optional)
    /// </summary>
    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }
}

/// <summary>
/// 视频URL对象<br />
/// Video URL object
/// </summary>
public record OpenAIVideoUrl
{
    /// <summary>
    /// 视频URL或data URI<br />
    /// Video URL or data URI
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }
}

/// <summary>
/// 输入音频对象<br />
/// Input audio object
/// </summary>
public record OpenAIInputAudio
{
    /// <summary>
    /// Base64编码的音频数据<br />
    /// Base64-encoded audio data
    /// </summary>
    [JsonPropertyName("data")]
    public required string Data { get; init; }

    /// <summary>
    /// 音频格式：wav, mp3<br />
    /// Audio format: wav, mp3
    /// </summary>
    [JsonPropertyName("format")]
    public required string Format { get; init; }
}
