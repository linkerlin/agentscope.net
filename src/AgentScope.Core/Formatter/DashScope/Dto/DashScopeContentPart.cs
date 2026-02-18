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
/// DashScope 多模态消息内容块
/// 
/// Java参考: io.agentscope.core.formatter.dashscope.dto.DashScopeContentPart
/// </summary>
public class DashScopeContentPart
{
    /// <summary>
    /// Text content
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    /// <summary>
    /// Image URL or base64 data URI
    /// </summary>
    [JsonPropertyName("image")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Image { get; set; }

    /// <summary>
    /// Audio URL or base64 data URI
    /// </summary>
    [JsonPropertyName("audio")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Audio { get; set; }

    /// <summary>
    /// Video URL or frame list
    /// </summary>
    [JsonPropertyName("video")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Video { get; set; }

    /// <summary>
    /// Create a text content part
    /// </summary>
    public static DashScopeContentPart FromText(string text) => new() { Text = text };

    /// <summary>
    /// Create an image content part
    /// </summary>
    public static DashScopeContentPart FromImage(string imageUrl) => new() { Image = imageUrl };

    /// <summary>
    /// Create an audio content part
    /// </summary>
    public static DashScopeContentPart FromAudio(string audioUrl) => new() { Audio = audioUrl };

    /// <summary>
    /// Create a video content part from URL
    /// </summary>
    public static DashScopeContentPart FromVideo(string videoUrl) => new() { Video = videoUrl };

    /// <summary>
    /// Create a video content part from frame list
    /// </summary>
    public static DashScopeContentPart FromVideoFrames(List<string> frames) => new() { Video = frames };
}
