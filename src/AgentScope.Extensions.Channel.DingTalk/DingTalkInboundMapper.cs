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
    /// <summary>Channel identifier / 渠道标识</summary>
    private readonly string _channelId;

    /// <summary>Account identifier (appKey) / 账户标识（appKey）</summary>
    private readonly string _accountId;

    /// <summary>
    /// Initializes a new instance of the <see cref="DingTalkInboundMapper"/> class.
    /// 初始化 <see cref="DingTalkInboundMapper"/> 类的新实例。
    /// </summary>
    /// <param name="channelId">Channel identifier / 渠道标识</param>
    /// <param name="accountId">Account identifier (appKey) / 账户标识（appKey）</param>
    public DingTalkInboundMapper(string channelId, string accountId)
    {
        _channelId = channelId;
        _accountId = accountId;
    }

    /// <summary>
    /// Maps a DingTalk payload to an inbound message; returns null for non-text or empty content.
    /// 映射为入站消息；非 text 或内容为空时返回 null。
    /// </summary>
    /// <param name="payload">DingTalk callback JSON payload / 钉钉回调 JSON 负载</param>
    /// <returns>Mapped inbound message, or null if not mappable / 映射后的入站消息，不可映射时返回 null</returns>
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

    /// <summary>
    /// Extracts the <c>msgId</c> field for idempotency deduplication; returns null if missing.
    /// 返回 <c>msgId</c>（幂等去重键），缺失时返回 null。
    /// </summary>
    /// <param name="payload">DingTalk callback JSON payload / 钉钉回调 JSON 负载</param>
    /// <returns>Message ID string, or null / 消息 ID 字符串，或 null</returns>
    public static string? ExtractMsgId(JsonNode? payload)
    {
        if (payload is null)
        {
            return null;
        }
        var id = TextValue(payload, "msgId");
        return string.IsNullOrWhiteSpace(id) ? null : id;
    }

    /// <summary>
    /// Safely extracts a string field from a JSON node.
    /// 安全地从 JSON 节点中提取字符串字段。
    /// </summary>
    /// <param name="node">Source JSON node / 源 JSON 节点</param>
    /// <param name="field">Field name / 字段名</param>
    /// <returns>The field value as string, or null if missing / 字段值的字符串形式，缺失时返回 null</returns>
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
