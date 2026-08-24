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
/// DashScope content part DTO for multimodal messages.
/// DashScope 多模态消息内容块 DTO，支持文本、图片、音频和视频等多种模态。
///
/// 仅当 content 为 List&lt;DashScopeContentPart&gt; 时，消息为多模态格式，
/// 否则 content 为纯文本字符串。
/// Only when content is List&lt;DashScopeContentPart&gt; is the message multimodal;
/// otherwise content is a plain text string.
///
/// Java参考: io.agentscope.core.formatter.dashscope.dto.DashScopeContentPart
/// </summary>
public class DashScopeContentPart
{
    /// <summary>
    /// 文本内容，用于纯文本部分。
    /// Text content for plain text parts.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    /// <summary>
    /// 图片 URL 或 base64 数据 URI，用于图片输入。
    /// Image URL or base64 data URI for image input.
    /// </summary>
    [JsonPropertyName("image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Image { get; set; }

    /// <summary>
    /// 音频 URL 或 base64 数据 URI，用于音频输入。
    /// Audio URL or base64 data URI for audio input.
    /// </summary>
    [JsonPropertyName("audio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Audio { get; set; }

    /// <summary>
    /// 视频 URL 或帧列表，用于视频输入。
    /// Video URL or frame list for video input.
    /// 可以是字符串（URL）或 List&lt;string&gt;（帧列表）。
    /// Can be a string (URL) or List&lt;string&gt; (frame list).
    /// </summary>
    [JsonPropertyName("video")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Video { get; set; }

    /// <summary>
    /// 从文本创建内容块。
    /// Create a text content part from the given text.
    /// </summary>
    /// <param name="text">文本内容 / Text content</param>
    /// <returns>文本内容块 / Text content part</returns>
    public static DashScopeContentPart FromText(string text) => new() { Text = text };

    /// <summary>
    /// 从图片 URL 创建内容块。
    /// Create an image content part from the given image URL.
    /// </summary>
    /// <param name="imageUrl">图片 URL / Image URL</param>
    /// <returns>图片内容块 / Image content part</returns>
    public static DashScopeContentPart FromImage(string imageUrl) => new() { Image = imageUrl };

    /// <summary>
    /// 从音频 URL 创建内容块。
    /// Create an audio content part from the given audio URL.
    /// </summary>
    /// <param name="audioUrl">音频 URL / Audio URL</param>
    /// <returns>音频内容块 / Audio content part</returns>
    public static DashScopeContentPart FromAudio(string audioUrl) => new() { Audio = audioUrl };

    /// <summary>
    /// 从视频 URL 创建内容块。
    /// Create a video content part from the given video URL.
    /// </summary>
    /// <param name="videoUrl">视频 URL / Video URL</param>
    /// <returns>视频内容块 / Video content part</returns>
    public static DashScopeContentPart FromVideo(string videoUrl) => new() { Video = videoUrl };

    /// <summary>
    /// 从视频帧列表创建内容块。
    /// Create a video content part from a list of frame URLs or base64 strings.
    /// </summary>
    /// <param name="frames">视频帧列表 / List of video frames</param>
    /// <returns>视频内容块 / Video content part</returns>
    public static DashScopeContentPart FromVideoFrames(List<string> frames) => new() { Video = frames };
}
