using AgentScope.Core.A2A.Client.Message;
using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Client.Utils;

/// <summary>
/// A2A 消息双向转换器。对标 Java MessageConvertUtil。
/// AgentScope Msg ↔ A2A Message / Artifact
/// </summary>
public sealed class MessageConvertUtil
{
    private readonly ContentBlockParserRouter _outboundParser;
    private readonly PartParserRouter _inboundParser;

    public MessageConvertUtil(ContentBlockParserRouter? outboundParser = null, PartParserRouter? inboundParser = null)
    {
        _outboundParser = outboundParser ?? ContentBlockParserRouter.CreateDefault();
        _inboundParser = inboundParser ?? new PartParserRouter();
    }

    /// <summary>Msg → A2A Message Parts</summary>
    public List<object> ConvertToParts(Msg msg)
    {
        var parts = new List<object>();
        if (msg.Content is ContentBlock single)
            parts.Add(_outboundParser.Parse(single));
        if (parts.Count == 0 && msg.GetTextContent() != null)
            parts.Add(new { type = "text", text = msg.GetTextContent() });
        return parts;
    }

    /// <summary>A2A Parts → Msg</summary>
    public Msg ConvertFromParts(IReadOnlyList<object> parts, string role = "assistant")
    {
        var builder = Msg.Builder().Role(role);
        foreach (var part in parts)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(part);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var kind = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() : "text";
            var block = _inboundParser.Parse(kind ?? "text", part);
            builder.Content(block);
        }
        return builder.Build();
    }

    /// <summary>流式块累加器。对标 Java StreamingChunkAccumulator。</summary>
    public sealed class StreamingChunkAccumulator
    {
        private readonly Dictionary<string, List<string>> _chunks = new();

        public void Accumulate(string msgId, string content)
        {
            if (!_chunks.ContainsKey(msgId))
                _chunks[msgId] = new List<string>();
            _chunks[msgId].Add(content);
        }

        public string GetAccumulated(string msgId) =>
            _chunks.TryGetValue(msgId, out var chunks) ? string.Concat(chunks) : "";
    }
}
