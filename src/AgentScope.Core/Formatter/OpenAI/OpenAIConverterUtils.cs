using System;
using System.IO;
using AgentScope.Core.Message;

namespace AgentScope.Core.Formatter.OpenAI;

/// <summary>
/// OpenAI 转换器工具类
/// OpenAI converter utility class
/// 
/// Java参考: io.agentscope.core.formatter.openai.OpenAIConverterUtils
/// </summary>
public static class OpenAIConverterUtils
{
    /// <summary>
    /// 将图片源转换为OpenAI URL格式
    /// Convert image source to OpenAI URL format
    /// 
    /// 支持URL和Base64数据URI
    /// Supports URL and Base64 data URI
    /// </summary>
    /// <param name="source">图片源URL或文件路径 / Image source URL or file path</param>
    /// <returns>OpenAI格式的图片URL / OpenAI formatted image URL</returns>
    public static string ConvertImageSourceToUrl(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Image source cannot be null or empty", nameof(source));
        }

        // 如果已经是URL或data URI，直接返回
        // If already a URL or data URI, return directly
        if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return source;
        }

        // 如果是本地文件路径，转换为data URI
        // If local file path, convert to data URI
        if (File.Exists(source))
        {
            var bytes = File.ReadAllBytes(source);
            var base64 = Convert.ToBase64String(bytes);
            var extension = Path.GetExtension(source).TrimStart('.').ToLowerInvariant();
            var mimeType = GetImageMimeType(extension);
            return $"data:{mimeType};base64,{base64}";
        }

        // 否则假设是Base64字符串，包装为data URI
        // Otherwise assume Base64 string, wrap as data URI
        return $"data:image/jpeg;base64,{source}";
    }

    /// <summary>
    /// 将视频源转换为OpenAI URL格式
    /// Convert video source to OpenAI URL format
    /// 
    /// 支持URL和Base64数据URI
    /// Supports URL and Base64 data URI
    /// </summary>
    /// <param name="source">视频源URL或文件路径 / Video source URL or file path</param>
    /// <returns>OpenAI格式的视频URL / OpenAI formatted video URL</returns>
    public static string ConvertVideoSourceToUrl(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Video source cannot be null or empty", nameof(source));
        }

        // 如果已经是URL或data URI，直接返回
        // If already a URL or data URI, return directly
        if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return source;
        }

        // 如果是本地文件路径，转换为data URI
        // If local file path, convert to data URI
        if (File.Exists(source))
        {
            var bytes = File.ReadAllBytes(source);
            var base64 = Convert.ToBase64String(bytes);
            var extension = Path.GetExtension(source).TrimStart('.').ToLowerInvariant();
            var mimeType = GetVideoMimeType(extension);
            return $"data:{mimeType};base64,{base64}";
        }

        // 否则假设是Base64字符串，包装为data URI
        // Otherwise assume Base64 string, wrap as data URI
        return $"data:video/mp4;base64,{source}";
    }

    /// <summary>
    /// 检测音频格式
    /// Detect audio format from media type
    /// </summary>
    /// <param name="mediaType">媒体类型 / Media type (e.g., "audio/wav", "audio/mp3")</param>
    /// <returns>音频格式 / Audio format ("wav" or "mp3")</returns>
    public static string DetectAudioFormat(string mediaType)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            return "wav"; // 默认格式 / Default format
        }

        var lowerMediaType = mediaType.ToLowerInvariant();

        if (lowerMediaType.Contains("wav"))
        {
            return "wav";
        }
        else if (lowerMediaType.Contains("mp3") || lowerMediaType.Contains("mpeg"))
        {
            return "mp3";
        }

        // 默认返回wav
        // Default to wav
        return "wav";
    }

    /// <summary>
    /// 根据文件扩展名获取图片MIME类型
    /// Get image MIME type from file extension
    /// </summary>
    private static string GetImageMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "webp" => "image/webp",
            "bmp" => "image/bmp",
            "svg" => "image/svg+xml",
            _ => "image/jpeg" // 默认MIME类型 / Default MIME type
        };
    }

    /// <summary>
    /// 根据文件扩展名获取视频MIME类型
    /// Get video MIME type from file extension
    /// </summary>
    private static string GetVideoMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            "mp4" => "video/mp4",
            "mpeg" or "mpg" => "video/mpeg",
            "mov" => "video/quicktime",
            "avi" => "video/x-msvideo",
            "wmv" => "video/x-ms-wmv",
            "flv" => "video/x-flv",
            "webm" => "video/webm",
            "mkv" => "video/x-matroska",
            _ => "video/mp4" // 默认MIME类型 / Default MIME type
        };
    }
}
