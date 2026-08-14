using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Client.Message;

/// <summary>
/// A2A Part → ContentBlock 解析器。对标 Java PartParser。
/// </summary>
public interface IPartParser
{
    ContentBlock Parse(object part);
}

/// <summary>
/// PartParser 路由器。对标 Java PartParserRouter。
/// </summary>
public sealed class PartParserRouter
{
    private readonly Dictionary<string, IPartParser> _parsers = new()
    {
        ["text"] = new TextPartParser(),
        ["file"] = new FilePartParser(),
        ["data"] = new DataPartParser()
    };

    public ContentBlock Parse(string kind, object part) =>
        _parsers.TryGetValue(kind, out var parser) ? parser.Parse(part) : new TextBlock { Text = part.ToString() };
}

public sealed class TextPartParser : IPartParser
{
    public ContentBlock Parse(object part)
    {
        var text = ExtractText(part);
        var meta = ExtractMetadata(part);
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

        if (mime.StartsWith("image")) return new ImageBlock { Url = uri ?? "", Data = bytes };
        if (mime.StartsWith("audio")) return new AudioBlock { Url = uri ?? "", Data = bytes };
        if (mime.StartsWith("video")) return new VideoBlock { Url = uri ?? "", Data = bytes };

        return new TextBlock { Text = $"[file: {mime}]" };
    }
}

public sealed class DataPartParser : IPartParser
{
    public ContentBlock Parse(object part)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(part);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

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
