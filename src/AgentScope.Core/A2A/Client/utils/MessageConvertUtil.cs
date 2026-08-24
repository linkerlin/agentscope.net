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

using AgentScope.Core.A2A.Client.Message;
using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Client.Utils;

/// <summary>
/// Bidirectional A2A message converter. Corresponds to Java MessageConvertUtil.
/// Converts between AgentScope Msg and A2A Message/Artifact formats.
/// A2A 消息双向转换器。对标 Java MessageConvertUtil。
/// AgentScope Msg ↔ A2A Message / Artifact
/// </summary>
public sealed class MessageConvertUtil
{
    private readonly ContentBlockParserRouter _outboundParser;
    private readonly PartParserRouter _inboundParser;

    /// <summary>
    /// Creates a converter with optional custom parsers.
    /// Defaults to built-in parsers for all known block/part types.
    /// 使用可选的定制解析器创建转换器。默认使用所有已知块/Part 类型的内置解析器。
    /// </summary>
    public MessageConvertUtil(ContentBlockParserRouter? outboundParser = null, PartParserRouter? inboundParser = null)
    {
        _outboundParser = outboundParser ?? ContentBlockParserRouter.CreateDefault();
        _inboundParser = inboundParser ?? new PartParserRouter();
    }

    /// <summary>
    /// Converts an AgentScope Msg to a list of A2A message Parts.
    /// 将 AgentScope Msg 转换为 A2A 消息 Parts 列表
    /// </summary>
    public List<object> ConvertToParts(Msg msg)
    {
        var parts = new List<object>();
        // If the message has structured content, parse each block
        // 如果消息有结构化内容，解析每个块
        if (msg.Content is ContentBlock single)
            parts.Add(_outboundParser.Parse(single));
        // Fallback: use plain text content
        // 回退：使用纯文本内容
        if (parts.Count == 0 && msg.GetTextContent() != null)
            parts.Add(new { type = "text", text = msg.GetTextContent() });
        return parts;
    }

    /// <summary>
    /// Converts a list of A2A Parts back to an AgentScope Msg.
    /// 将 A2A Parts 列表转换回 AgentScope Msg
    /// </summary>
    public Msg ConvertFromParts(IReadOnlyList<object> parts, string role = "assistant")
    {
        var builder = Msg.Builder().Role(role);
        foreach (var part in parts)
        {
            // Serialize to JSON to inspect the "type" field
            // 序列化为 JSON 以检查 "type" 字段
            var json = System.Text.Json.JsonSerializer.Serialize(part);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var kind = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() : "text";
            var block = _inboundParser.Parse(kind ?? "text", part);
            builder.Content(block);
        }
        return builder.Build();
    }

    /// <summary>
    /// Streaming chunk accumulator. Corresponds to Java StreamingChunkAccumulator.
    /// 流式块累加器。对标 Java StreamingChunkAccumulator。
    /// </summary>
    public sealed class StreamingChunkAccumulator
    {
        // Stores chunks grouped by message ID
        // 按消息 ID 分组存储块数据
        private readonly Dictionary<string, List<string>> _chunks = new();

        /// <summary>
        /// Accumulates a content chunk for the given message ID.
        /// 累加指定消息 ID 的内容块
        /// </summary>
        public void Accumulate(string msgId, string content)
        {
            if (!_chunks.ContainsKey(msgId))
                _chunks[msgId] = new List<string>();
            _chunks[msgId].Add(content);
        }

        /// <summary>
        /// Gets the concatenated content for the given message ID.
        /// 获取指定消息 ID 的拼接后内容
        /// </summary>
        public string GetAccumulated(string msgId) =>
            _chunks.TryGetValue(msgId, out var chunks) ? string.Concat(chunks) : "";
    }
}
