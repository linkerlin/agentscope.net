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

using System.Xml;
using System.Xml.Linq;
using AgentScope.Extensions.Channel;

namespace AgentScope.Extensions.Channel.WeCom;

/// <summary>
/// 将解密后的企业微信回调 XML 映射为 <see cref="InboundMessage"/>。
/// 对应 Java: io.agentscope.extensions.channel.wecom.WeComInboundMapper
/// </summary>
/// <remarks>MVP 仅映射 <c>MsgType=text</c>。</remarks>
public sealed class WeComInboundMapper
{
    private readonly string _channelId;
    private readonly string _accountId;

    public WeComInboundMapper(string channelId, string accountId)
    {
        _channelId = channelId;
        _accountId = accountId;
    }

    /// <summary>映射为入站消息；非 text 或内容缺失时返回 null。</summary>
    public InboundMessage? Map(string xml)
    {
        XDocument doc;
        try
        {
            doc = ParseXml(xml);
        }
        catch (XmlException)
        {
            throw new InvalidOperationException("Failed to parse WeCom callback XML");
        }
        var root = doc.Root;
        if (root is null)
        {
            return null;
        }

        var msgType = ElementValue(root, "MsgType");
        if (!string.Equals(msgType, "text", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        var fromUser = ElementValue(root, "FromUserName");
        var content = ElementValue(root, "Content");
        if (string.IsNullOrWhiteSpace(fromUser) || content is null)
        {
            return null;
        }

        var metadata = new Dictionary<string, object>
        {
            ["peer"] = fromUser,
            ["senderId"] = fromUser,
            ["accountId"] = _accountId,
        };
        return new InboundMessage(fromUser, content, _channelId, metadata);
    }

    /// <summary>返回 <c>MsgId</c>（幂等去重键），缺失时返回 null。</summary>
    public static string? ExtractMsgId(string xml)
    {
        try
        {
            var doc = ParseXml(xml);
            var id = doc.Root is null ? null : ElementValue(doc.Root, "MsgId");
            return string.IsNullOrWhiteSpace(id) ? null : id;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    /// <summary>从外层回调 XML 中提取 <c>Encrypt</c> 字段，缺失时返回 null。</summary>
    internal static string? ExtractEncrypt(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return null;
        }
        try
        {
            var doc = ParseXml(xml);
            var v = doc.Root is null ? null : ElementValue(doc.Root, "Encrypt");
            return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        }
        catch (XmlException)
        {
            return null;
        }
    }

    /// <summary>以禁止 DTD 的安全方式解析 XML（防 XXE）。</summary>
    private static XDocument ParseXml(string xml)
    {
        using var reader = XmlReader.Create(
            new StringReader(xml),
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static string? ElementValue(XElement? root, string name)
    {
        return root?.Element(name)?.Value;
    }
}
