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

using System.Text.Json;
using System.Text.Json.Nodes;
using AgentScope.Extensions.Channel;

namespace AgentScope.Extensions.Channel.Feishu;

/// <summary>
/// 将解密后的飞书事件订阅 v2 envelope 映射为 <see cref="InboundMessage"/>。
/// 对应 Java: io.agentscope.extensions.channel.feishu.FeishuInboundMapper
/// </summary>
/// <remarks>
/// MVP 仅映射 <c>message_type=text</c>。内层 <c>content</c> 是 JSON 字符串，需二次解析读 <c>text</c> 字段。
/// </remarks>
public sealed class FeishuInboundMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly string _channelId;

    public FeishuInboundMapper(string channelId)
    {
        _channelId = channelId;
    }

    /// <summary>返回 <c>header.event_id</c>（幂等去重键），缺失时返回 null。</summary>
    public static string? ExtractEventId(JsonNode? envelope)
    {
        if (envelope is null)
        {
            return null;
        }
        var id = TextValue(envelope["header"], "event_id");
        return string.IsNullOrWhiteSpace(id) ? null : id;
    }

    /// <summary>若 envelope 为 url_verification 请求，返回 challenge；否则返回 null。</summary>
    public static string? ExtractUrlChallenge(JsonNode? envelope)
    {
        if (envelope is null)
        {
            return null;
        }
        var type = TextValue(envelope, "type");
        if (!string.Equals(type, "url_verification", StringComparison.Ordinal))
        {
            return null;
        }
        var challenge = TextValue(envelope, "challenge");
        return string.IsNullOrWhiteSpace(challenge) ? null : challenge;
    }

    /// <summary>返回 <c>event.sender.sender_id.open_id</c>（发送者 open_id）。</summary>
    public static string? ExtractSenderOpenId(JsonNode? envelope)
    {
        if (envelope is null)
        {
            return null;
        }
        var id = TextValue(envelope["event"]?["sender"]?["sender_id"], "open_id");
        return string.IsNullOrWhiteSpace(id) ? null : id;
    }

    /// <summary>返回 <c>header.tenant_key</c>（多租户 id）。</summary>
    public static string? ExtractTenantKey(JsonNode? envelope)
    {
        if (envelope is null)
        {
            return null;
        }
        var key = TextValue(envelope["header"], "tenant_key");
        return string.IsNullOrWhiteSpace(key) ? null : key;
    }

    /// <summary>映射为入站消息；非 text 或格式非法时返回 null。</summary>
    public InboundMessage? Map(JsonNode? envelope)
    {
        if (envelope is null)
        {
            return null;
        }

        var eventNode = envelope["event"];
        if (eventNode is not JsonObject)
        {
            return null;
        }

        var message = eventNode["message"];
        var messageType = TextValue(message, "message_type");
        if (!string.Equals(messageType, "text", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var chatType = TextValue(message, "chat_type");
        var chatId = TextValue(message, "chat_id");
        if (string.IsNullOrWhiteSpace(chatId))
        {
            return null;
        }

        var openId = TextValue(eventNode["sender"]?["sender_id"], "open_id");
        var contentJson = TextValue(message, "content");
        if (string.IsNullOrWhiteSpace(contentJson))
        {
            return null;
        }

        string? text;
        try
        {
            var content = JsonNode.Parse(contentJson);
            text = TextValue(content, "text");
        }
        catch (JsonException)
        {
            return null;
        }
        if (text is null)
        {
            return null;
        }

        var senderName = openId ?? chatId;
        var tenant = TextValue(envelope["header"], "tenant_key");

        var metadata = new Dictionary<string, object>
        {
            ["peer"] = chatId,
            ["senderId"] = senderName,
            ["chatType"] = chatType ?? "",
        };
        if (tenant is not null)
        {
            metadata["accountId"] = tenant;
        }
        return new InboundMessage(senderName, text, _channelId, metadata);
    }

    private static string? TextValue(JsonNode? node, string field)
    {
        var v = node?[field];
        if (v is null)
        {
            return null;
        }
        return v.GetValueKind() == JsonValueKind.String ? v.GetValue<string>() : v.ToString();
    }
}
