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

namespace AgentScope.Extensions.Channel.GitHub;

/// <summary>
/// 将 GitHub webhook 投递映射为 <see cref="InboundMessage"/>。
/// 对应 Java: io.agentscope.extensions.channel.github.GitHubInboundMapper
/// </summary>
/// <remarks>
/// MVP 处理两类事件：<c>issue_comment</c> 与 <c>pull_request_review_comment</c>。
/// peer = <c>&lt;owner&gt;/&lt;repo&gt;#&lt;number&gt;</c>（出站客户端可据此构建评论 URL）。
/// </remarks>
public sealed class GitHubInboundMapper
{
    private readonly string _channelId;

    public GitHubInboundMapper(string channelId)
    {
        _channelId = channelId;
    }

    /// <summary>返回 <c>comment.id</c>（幂等去重键），缺失时返回 null。</summary>
    public static long? ExtractCommentId(JsonNode? payload)
    {
        if (payload is null)
        {
            return null;
        }
        long id = LongValue(payload["comment"], "id");
        return id > 0 ? id : null;
    }

    /// <summary>返回评论作者的数值 id（用于 bot-loop 自检）。</summary>
    public static long? ExtractCommenterId(JsonNode? payload)
    {
        if (payload is null)
        {
            return null;
        }
        long id = LongValue(payload["comment"]?["user"], "id");
        return id > 0 ? id : null;
    }

    /// <summary>映射为入站消息；非 created 动作或格式非法时返回 null。</summary>
    public InboundMessage? Map(string? eventType, JsonNode? payload)
    {
        if (payload is null)
        {
            return null;
        }

        // 仅处理新建评论；编辑/删除在 MVP 中丢弃。
        var action = TextValue(payload, "action");
        if (!string.Equals(action, "created", StringComparison.Ordinal))
        {
            return null;
        }

        var repo = payload["repository"];
        var fullName = TextValue(repo, "full_name");
        var ownerLogin = TextValue(repo?["owner"], "login");
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return null;
        }

        long number;
        if (string.Equals(eventType, "issue_comment", StringComparison.Ordinal))
        {
            number = LongValue(payload["issue"], "number");
        }
        else if (string.Equals(eventType, "pull_request_review_comment", StringComparison.Ordinal))
        {
            number = LongValue(payload["pull_request"], "number");
        }
        else
        {
            return null;
        }
        if (number <= 0)
        {
            return null;
        }

        var comment = payload["comment"];
        var body = TextValue(comment, "body");
        var authorLogin = TextValue(comment?["user"], "login");
        if (body is null || authorLogin is null)
        {
            return null;
        }

        var peerId = fullName + "#" + number;
        var metadata = new Dictionary<string, object>
        {
            ["peer"] = peerId,
            ["senderId"] = authorLogin,
        };
        if (ownerLogin is not null)
        {
            metadata["accountId"] = ownerLogin;
        }
        return new InboundMessage(authorLogin, body, _channelId, metadata);
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

    private static long LongValue(JsonNode? node, string field, long fallback = -1)
    {
        var v = node?[field];
        if (v is null)
        {
            return fallback;
        }
        if (v.GetValueKind() == JsonValueKind.Number)
        {
            try
            {
                return v.GetValue<long>();
            }
            catch (InvalidOperationException)
            {
                // fall through to fallback
            }
        }
        else if (v.GetValueKind() == JsonValueKind.String && long.TryParse(v.GetValue<string>(), out var l))
        {
            return l;
        }
        return fallback;
    }
}
