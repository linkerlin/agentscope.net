using System.Text.Json.Serialization;

namespace AgentScope.Core.Formatter.OpenAI.Dto;

/// <summary>
/// OpenAI 内容部分基类
/// Base class for OpenAI content parts
/// 
/// Java参考: io.agentscope.core.formatter.openai.dto.OpenAIContentPart
/// </summary>
public abstract record OpenAIContentPart
{
    /// <summary>
    /// 内容类型
    /// Content type
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }
}

/// <summary>
/// 文本内容部分
/// Text content part
/// </summary>
public record TextContentPart : OpenAIContentPart
{
    /// <summary>
    /// 文本内容
    /// Text content
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    /// <summary>
    /// 创建文本内容部分
    /// Create text content part
    /// </summary>
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
/// 图片URL内容部分
/// Image URL content part
/// </summary>
public record ImageContentPart : OpenAIContentPart
{
    /// <summary>
    /// 图片URL信息
    /// Image URL information
    /// </summary>
    [JsonPropertyName("image_url")]
    public required ImageUrl ImageUrl { get; init; }

    /// <summary>
    /// 创建图片内容部分
    /// Create image content part
    /// </summary>
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
/// 图片URL信息
/// Image URL information
/// </summary>
public record ImageUrl
{
    /// <summary>
    /// 图片URL或data URI
    /// Image URL or data URI
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// 细节级别：auto, low, high
    /// Detail level: auto, low, high
    /// </summary>
    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }
}

/// <summary>
/// 视频URL内容部分
/// Video URL content part
/// </summary>
public record VideoContentPart : OpenAIContentPart
{
    /// <summary>
    /// 视频URL信息
    /// Video URL information
    /// </summary>
    [JsonPropertyName("video_url")]
    public required VideoUrl VideoUrl { get; init; }

    /// <summary>
    /// 创建视频内容部分
    /// Create video content part
    /// </summary>
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
/// 视频URL信息
/// Video URL information
/// </summary>
public record VideoUrl
{
    /// <summary>
    /// 视频URL或data URI
    /// Video URL or data URI
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }

    /// <summary>
    /// 视频格式：mp4, mpeg, mpg, mov, avi, wmv, flv, webm, mkv
    /// Video format: mp4, mpeg, mpg, mov, avi, wmv, flv, webm, mkv
    /// </summary>
    [JsonPropertyName("format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Format { get; init; }
}

/// <summary>
/// 音频输入内容部分
/// Audio input content part
/// </summary>
public record InputAudioContentPart : OpenAIContentPart
{
    /// <summary>
    /// 输入音频信息
    /// Input audio information
    /// </summary>
    [JsonPropertyName("input_audio")]
    public required InputAudio InputAudio { get; init; }

    /// <summary>
    /// 创建音频输入内容部分
    /// Create audio input content part
    /// </summary>
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
/// 输入音频信息
/// Input audio information
/// </summary>
public record InputAudio
{
    /// <summary>
    /// Base64编码的音频数据
    /// Base64-encoded audio data
    /// </summary>
    [JsonPropertyName("data")]
    public required string Data { get; init; }

    /// <summary>
    /// 音频格式：wav, mp3
    /// Audio format: wav, mp3
    /// </summary>
    [JsonPropertyName("format")]
    public required string Format { get; init; }
}
