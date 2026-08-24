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

using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Client.Message;

/// <summary>
/// A2A Part to ContentBlock parser. Corresponds to Java PartParser.
/// A2A Part → ContentBlock 解析器。对标 Java PartParser。
/// </summary>
public interface IPartParser
{
    /// <summary>
    /// Parses an A2A Part into a ContentBlock.
    /// 将 A2A Part 解析为 ContentBlock
    /// </summary>
    ContentBlock Parse(object part);
}

/// <summary>
/// PartParser router. Corresponds to Java PartParserRouter.
/// PartParser 路由器。对标 Java PartParserRouter。
/// </summary>
public sealed class PartParserRouter
{
    // Built-in parsers keyed by A2A Part type
    // 内置解析器，以 A2A Part 类型为键
    private readonly Dictionary<string, IPartParser> _parsers = new()
    {
        ["text"] = new TextPartParser(),
        ["file"] = new FilePartParser(),
        ["data"] = new DataPartParser()
    };

    /// <summary>
    /// Parses an A2A Part by dispatching to the parser for its kind.
    /// Falls back to a TextBlock if no parser is registered.
    /// 根据 Part 类型分发到对应解析器。未注册时回退为 TextBlock。
    /// </summary>
    public ContentBlock Parse(string kind, object part) =>
        _parsers.TryGetValue(kind, out var parser) ? parser.Parse(part) : new TextBlock { Text = part.ToString() };
}

/// <summary>
/// Parses A2A text parts. Handles thinking blocks via metadata discrimination.
/// 解析 A2A 文本 Part。通过元数据区分思考块（thinking block）。
/// </summary>
public sealed class TextPartParser : IPartParser
{
    public ContentBlock Parse(object part)
    {
        var text = ExtractText(part);
        var meta = ExtractMetadata(part);
        // Check if this text part is actually a thinking block
        // 检查该文本 Part 是否为思考块
        if (meta.TryGetValue(MessageConstants.MetaBlockType, out var bt) && bt?.ToString() == "thinking")
            return new ThinkingBlock { Thinking = text };
        return new TextBlock { Text = text };
    }

    private static string ExtractText(object part)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(part);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("text", out var t) ? t.GetString() ?? "" : json;
    }

    private static Dictionary<string, object> ExtractMetadata(object part) => new();
}

/// <summary>
/// Parses A2A file parts into image/audio/video blocks based on MIME type.
/// 根据 MIME 类型将 A2A 文件 Part 解析为图片/音频/视频块。
/// </summary>
public sealed class FilePartParser : IPartParser
{
    public ContentBlock Parse(object part)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(part);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        var mime = root.TryGetProperty("mimeType", out var m) ? m.GetString() ?? "" : "";
        var bytes = root.TryGetProperty("bytes", out var b) ? b.GetBytesFromBase64() : null;
        var uri = root.TryGetProperty("uri", out var u) ? u.GetString() : null;

        // Route by MIME type prefix
        // 根据 MIME 类型前缀路由
        if (mime.StartsWith("image")) return new ImageBlock { Url = uri ?? "", Data = bytes };
        if (mime.StartsWith("audio")) return new AudioBlock { Url = uri ?? "", Data = bytes };
        if (mime.StartsWith("video")) return new VideoBlock { Url = uri ?? "", Data = bytes };

        return new TextBlock { Text = $"[file: {mime}]" };
    }
}

/// <summary>
/// Parses A2A data parts. Handles tool_use and tool_result blocks via metadata.
/// 解析 A2A 数据 Part。通过元数据处理 tool_use 和 tool_result 块。
/// </summary>
public sealed class DataPartParser : IPartParser
{
    public ContentBlock Parse(object part)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(part);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Read metadata to determine block type
        // 读取元数据以确定块类型
        var meta = root.TryGetProperty("metadata", out var md) ? md : default;
        var blockType = meta.TryGetProperty(MessageConstants.MetaBlockType, out var bt) ? bt.GetString() : "";

        if (blockType == "tool_use")
        {
            var name = meta.TryGetProperty(MessageConstants.MetaToolName, out var n) ? n.GetString() : "";
            var id = meta.TryGetProperty(MessageConstants.MetaToolCallId, out var c) ? c.GetString() : "";
            return new ToolUseBlock { Name = name ?? "", Id = id ?? "" };
        }

        if (blockType == "tool_result")
        {
            var id = meta.TryGetProperty(MessageConstants.MetaToolCallId, out var c) ? c.GetString() : "";
            return new ToolResultBlock { Id = id ?? "", Output = root.TryGetProperty("data", out var d) ? d.ToString() : "" };
        }

        return new TextBlock { Text = root.TryGetProperty("data", out var dt) ? dt.ToString() : "" };
    }
}
