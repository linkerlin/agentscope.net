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

namespace AgentScope.Core.Formatter.OpenAI.Dto;

/// <summary>
/// OpenAI 内容部分基类<br />
/// Base class for OpenAI content parts<br />
/// Java参考: io.agentscope.core.formatter.openai.dto.OpenAIContentPart
/// </summary>
public abstract record OpenAIContentPart
{
    /// <summary>
    /// 内容类型（如 text, image_url, video_url, input_audio）<br />
    /// Content type (e.g. text, image_url, video_url, input_audio)
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }
}

/// <summary>
/// 文本内容部分，type = "text"<br />
/// Text content part, type = "text"
/// </summary>
public record TextContentPart : OpenAIContentPart
{
    /// <summary>
    /// 文本内容<br />
    /// Text content
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    /// <summary>
    /// 创建文本内容部分的工厂方法<br />
    /// Factory method to create a text content part
    /// </summary>
    /// <param name="text">文本内容 / Text content</param>
    /// <returns>文本内容部分实例 / A new TextContentPart instance</returns>
    public static TextContentPart Create(string text)
    {
        return new TextContentPart
        {
            Type = "text",
            Text = text
        };
    }
}

/// <summary>
/// 图片URL内容部分，type = "image_url"<br />
/// Image URL content part, type = "image_url"
/// </summary>
public record ImageContentPart : OpenAIContentPart
{
    /// <summary>
    /// 图片URL信息<br />
    /// Image URL information
    /// </summary>
    [JsonPropertyName("image_url")]
    public required ImageUrl ImageUrl { get; init; }

    /// <summary>
    /// 创建图片内容部分的工厂方法<br />
    /// Factory method to create an image content part
    /// </summary>
    /// <param name="url">图片URL或data URI / Image URL or data URI</param>
    /// <param name="detail">细节级别（auto/low/high，可选）/ Detail level (auto/low/high, optional)</param>
    /// <returns>图片内容部分实例 / A new ImageContentPart instance</returns>
    public static ImageContentPart Create(string url, string? detail = null)
    {
        return new ImageContentPart
        {
            Type = "image_url",
            ImageUrl = new ImageUrl
            {
                Url = url,
                Detail = detail
            }
        };
    }
}

/// <summary>
/// 图片URL信息对象<br />
/// Image URL information object
/// </summary>
public record ImageUrl
{
    /// <summary>
    /// 图片URL或data URI<br />
    /// Image URL or data URI
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// 细节级别：auto, low, high（可选）<br />
    /// Detail level: auto, low, high (optional)
    /// </summary>
    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }
}

/// <summary>
/// 视频URL内容部分，type = "video_url"<br />
/// Video URL content part, type = "video_url"
/// </summary>
public record VideoContentPart : OpenAIContentPart
{
    /// <summary>
    /// 视频URL信息<br />
    /// Video URL information
    /// </summary>
    [JsonPropertyName("video_url")]
    public required VideoUrl VideoUrl { get; init; }

    /// <summary>
    /// 创建视频内容部分的工厂方法<br />
    /// Factory method to create a video content part
    /// </summary>
    /// <param name="url">视频URL或data URI / Video URL or data URI</param>
    /// <param name="format">视频格式（如 mp4, webm, 可选）/ Video format (e.g. mp4, webm, optional)</param>
    /// <returns>视频内容部分实例 / A new VideoContentPart instance</returns>
    public static VideoContentPart Create(string url, string? format = null)
    {
        return new VideoContentPart
        {
            Type = "video_url",
            VideoUrl = new VideoUrl
            {
                Url = url,
                Format = format
            }
        };
    }
}

/// <summary>
/// 视频URL信息对象<br />
/// Video URL information object
/// </summary>
public record VideoUrl
{
    /// <summary>
    /// 视频URL或data URI<br />
    /// Video URL or data URI
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// 视频格式：mp4, mpeg, mpg, mov, avi, wmv, flv, webm, mkv（可选）<br />
    /// Video format: mp4, mpeg, mpg, mov, avi, wmv, flv, webm, mkv (optional)
    /// </summary>
    [JsonPropertyName("format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Format { get; init; }
}

/// <summary>
/// 音频输入内容部分，type = "input_audio"<br />
/// Audio input content part, type = "input_audio"
/// </summary>
public record InputAudioContentPart : OpenAIContentPart
{
    /// <summary>
    /// 输入音频信息<br />
    /// Input audio information
    /// </summary>
    [JsonPropertyName("input_audio")]
    public required InputAudio InputAudio { get; init; }

    /// <summary>
    /// 创建音频输入内容部分的工厂方法<br />
    /// Factory method to create an audio input content part
    /// </summary>
    /// <param name="data">Base64编码的音频数据 / Base64-encoded audio data</param>
    /// <param name="format">音频格式（wav/mp3）/ Audio format (wav/mp3)</param>
    /// <returns>音频输入内容部分实例 / A new InputAudioContentPart instance</returns>
    public static InputAudioContentPart Create(string data, string format)
    {
        return new InputAudioContentPart
        {
            Type = "input_audio",
            InputAudio = new InputAudio
            {
                Data = data,
                Format = format
            }
        };
    }
}

/// <summary>
/// 输入音频信息对象<br />
/// Input audio information object
/// </summary>
public record InputAudio
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
