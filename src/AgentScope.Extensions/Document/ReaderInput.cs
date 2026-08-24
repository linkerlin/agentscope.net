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

namespace AgentScope.Extensions.Document;

/// <summary>
/// Reader input specification. Maps to Java ReaderInput.
/// 读取器输入。对标 Java ReaderInput。
/// </summary>
public sealed record ReaderInput
{
    /// <summary>The type of input source (String, File, or Url). 输入源类型（String、File 或 Url）。</summary>
    public InputType Type { get; init; }

    /// <summary>The content or URL string. 内容或 URL 字符串。</summary>
    public string Content { get; init; } = "";

    /// <summary>The file path (if Type is File). 文件路径（如果 Type 为 File）。</summary>
    public string? FilePath { get; init; }

    /// <summary>Creates a ReaderInput from a plain text string. 从纯文本字符串创建 ReaderInput。</summary>
    /// <param name="text">The text content. 文本内容。</param>
    public static ReaderInput FromString(string text) =>
        new() { Type = InputType.String, Content = text };

    /// <summary>Creates a ReaderInput from a file path, reading its content. 从文件路径创建 ReaderInput，读取其内容。</summary>
    /// <param name="path">The file path. 文件路径。</param>
    public static ReaderInput FromFile(string path) =>
        new() { Type = InputType.File, FilePath = path, Content = File.ReadAllText(path) };

    /// <summary>Creates a ReaderInput from a URL string. 从 URL 字符串创建 ReaderInput。</summary>
    /// <param name="url">The URL. URL 地址。</param>
    public static ReaderInput FromUrl(string url) =>
        new() { Type = InputType.Url, Content = url };

    /// <summary>
    /// Input source types.
    /// 输入源类型。
    /// </summary>
    public enum InputType { String, File, Url }
}
