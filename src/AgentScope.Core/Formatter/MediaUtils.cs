// Copyright 2024-2026 the original author or authors.
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

using System;
using System.IO;

namespace AgentScope.Core.Formatter;

/// <summary>
/// 媒体工具类：MIME 推断、Base64 编解码、数据 URI 与 URL 判定。
/// 对应 Java: io.agentscope.core.formatter.MediaUtils
/// </summary>
public static class MediaUtils
{
    /// <summary>把字节数据编码为 Base64 字符串。</summary>
    public static string ToBase64(byte[] data) =>
        data == null ? "" : System.Convert.ToBase64String(data);

    /// <summary>从 Base64 字符串解码为字节数据。</summary>
    public static byte[] FromBase64(string base64)
    {
        if (string.IsNullOrEmpty(base64)) return System.Array.Empty<byte>();
        return System.Convert.FromBase64String(base64);
    }

    /// <summary>把字节数据包装为 data URI。</summary>
    public static string ToDataUri(byte[] data, string mimeType)
    {
        if (data == null) return "";
        return $"data:{mimeType};base64,{ToBase64(data)}";
    }

    /// <summary>判断字符串是否为 data URI。</summary>
    public static bool IsDataUri(string? value) =>
        !string.IsNullOrEmpty(value) && value.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    /// <summary>判断字符串是否为 URL（http/https/ftp）。</summary>
    public static bool IsUrl(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>根据文件扩展名推断 MIME 类型。</summary>
    public static string GuessMimeType(string? fileNameOrPath)
    {
        if (string.IsNullOrEmpty(fileNameOrPath)) return "application/octet-stream";
        var ext = Path.GetExtension(fileNameOrPath).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".html" or ".htm" => "text/html",
            ".csv" => "text/csv",
            ".md" => "text/markdown",
            ".xml" => "application/xml",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            _ => "application/octet-stream"
        };
    }
}
