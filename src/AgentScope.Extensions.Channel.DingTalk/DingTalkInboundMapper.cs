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

namespace AgentScope.Extensions.Channel.DingTalk;

/// <summary>
/// 将钉钉 Stream 机器人消息 payload 映射为 <see cref="InboundMessage"/>。
/// 对应 Java: io.agentscope.extensions.channel.dingtalk.DingTalkInboundMapper
/// </summary>
/// <remarks>
/// MVP 仅映射 <c>msgtype=text</c>。会话类型：
/// <c>conversationType="1"</c> → 单聊；<c>conversationType="2"</c> → 群聊（peer 为 conversationId）。
/// </remarks>
public sealed class DingTalkInboundMapper
{
    private readonly string _channelId;
    private readonly string _accountId;

    public DingTalkInboundMapper(string channelId, string accountId)
    {
        _channelId = channelId;
        _accountId = accountId;
    }

    /// <summary>映射为入站消息；非 text 或内容为空时返回 null。</summary>
    public InboundMessage? Map(JsonNode? payload)
    {
        if (payload is null)
        {
            return null;
        }

        var msgType = TextValue(payload, "msgtype");
        if (!string.Equals(msgType, "text", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var content = TextValue(payload["text"], "content");
        if (content is null)
        {
            return null;
        }
        content = content.Trim();
        if (content.Length == 0)
        {
            return null;
        }

        var conversationType = TextValue(payload, "conversationType");
        var senderStaffId = TextValue(payload, "senderStaffId");
        var conversationId = TextValue(payload, "conversationId");

        string peer;
        string senderId;
        if (conversationType == "2" && conversationId is not null)
        {
            peer = conversationId;
            senderId = senderStaffId ?? conversationId;
        }
        else
        {
            var peerId = senderStaffId ?? conversationId;
            if (string.IsNullOrWhiteSpace(peerId))
            {
                return null;
            }
            peer = peerId;
            senderId = peerId;
        }

        var metadata = new Dictionary<string, object>
        {
            ["peer"] = peer,
            ["senderId"] = senderId,
            ["accountId"] = _accountId,
        };
        return new InboundMessage(senderId, content, _channelId, metadata);
    }

    /// <summary>返回 <c>msgId</c>（幂等去重键），缺失时返回 null。</summary>
    public static string? ExtractMsgId(JsonNode? payload)
    {
        if (payload is null)
        {
            return null;
        }
        var id = TextValue(payload, "msgId");
        return string.IsNullOrWhiteSpace(id) ? null : id;
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
