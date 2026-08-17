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

using System;
using System.IO;

namespace AgentScope.Core.Formatter;

/// <summary>
/// Media utility class: MIME type inference, Base64 encoding/decoding, data URI and URL detection.
/// 媒体工具类：MIME 推断、Base64 编解码、数据 URI 与 URL 判定。
/// Corresponds to: io.agentscope.core.formatter.MediaUtils (Java)
/// </summary>
public static class MediaUtils
{
    /// <summary>
    /// Encodes byte data to a Base64 string.
    /// 把字节数据编码为 Base64 字符串。
    /// </summary>
    /// <param name="data">Byte data / 字节数据</param>
    /// <returns>Base64 encoded string / Base64 编码字符串</returns>
    public static string ToBase64(byte[] data) =>
        data == null ? "" : System.Convert.ToBase64String(data);

    /// <summary>
    /// Decodes a Base64 string to byte data.
    /// 从 Base64 字符串解码为字节数据。
    /// </summary>
    /// <param name="base64">Base64 encoded string / Base64 编码字符串</param>
    /// <returns>Decoded byte array / 解码后的字节数组</returns>
    public static byte[] FromBase64(string base64)
    {
        if (string.IsNullOrEmpty(base64)) return System.Array.Empty<byte>();
        return System.Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Wraps byte data into a data URI.
    /// 把字节数据包装为 data URI。
    /// </summary>
    /// <param name="data">Byte data / 字节数据</param>
    /// <param name="mimeType">MIME type / MIME 类型</param>
    /// <returns>Data URI string / Data URI 字符串</returns>
    public static string ToDataUri(byte[] data, string mimeType)
    {
        if (data == null) return "";
        return $"data:{mimeType};base64,{ToBase64(data)}";
    }

    /// <summary>
    /// Checks if the string is a data URI.
    /// 判断字符串是否为 data URI。
    /// </summary>
    /// <param name="value">String value / 字符串值</param>
    /// <returns>True if it is a data URI / 如果是 data URI 则返回 true</returns>
    public static bool IsDataUri(string? value) =>
        !string.IsNullOrEmpty(value) && value.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the string is a URL (http/https/ftp).
    /// 判断字符串是否为 URL（http/https/ftp）。
    /// </summary>
    /// <param name="value">String value / 字符串值</param>
    /// <returns>True if it is a URL / 如果是 URL 则返回 true</returns>
    public static bool IsUrl(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Infers MIME type from file extension.
    /// 根据文件扩展名推断 MIME 类型。
    /// </summary>
    /// <param name="fileNameOrPath">File name or path / 文件名或路径</param>
    /// <returns>Inferred MIME type / 推断的 MIME 类型</returns>
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
