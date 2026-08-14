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

namespace AgentScope.Extensions.Channel.GitLab;

/// <summary>
/// 将 GitLab Note Hook payload 映射为 <see cref="InboundMessage"/>。
/// 对应 Java: io.agentscope.extensions.channel.gitlab.GitLabInboundMapper
/// </summary>
/// <remarks>
/// MVP 仅映射 Issue 与 MergeRequest note；Commit/Snippet note 被丢弃。
/// 系统 note（标签/状态变更）通过 <c>object_attributes.system</c> 跳过。
/// peer = <c>&lt;project.path_with_namespace&gt;#&lt;iid&gt;:&lt;noteable_type&gt;</c>。
/// </remarks>
public sealed class GitLabInboundMapper
{
    private readonly string _channelId;

    public GitLabInboundMapper(string channelId)
    {
        _channelId = channelId;
    }

    /// <summary>返回 <c>object_attributes.id</c>（幂等去重键），缺失时返回 null。</summary>
    public static long? ExtractNoteId(JsonNode? payload)
    {
        if (payload is null)
        {
            return null;
        }
        long id = LongValue(payload["object_attributes"], "id");
        return id > 0 ? id : null;
    }

    /// <summary>返回 <c>user.id</c>（用于 bot-loop 自检）。</summary>
    public static long? ExtractAuthorId(JsonNode? payload)
    {
        if (payload is null)
        {
            return null;
        }
        long id = LongValue(payload["user"], "id");
        return id > 0 ? id : null;
    }

    /// <summary>映射为入站消息；系统 note、不支持的 noteable 类型或格式非法时返回 null。</summary>
    public InboundMessage? Map(JsonNode? payload)
    {
        if (payload is null)
        {
            return null;
        }

        var attrs = payload["object_attributes"];
        if (attrs is not JsonObject)
        {
            return null;
        }
        if (BoolValue(attrs, "system"))
        {
            return null;
        }

        var noteableType = TextValue(attrs, "noteable_type");
        if (!string.Equals(noteableType, "Issue", StringComparison.Ordinal)
            && !string.Equals(noteableType, "MergeRequest", StringComparison.Ordinal))
        {
            return null;
        }

        long iid = string.Equals(noteableType, "Issue", StringComparison.Ordinal)
            ? LongValue(payload["issue"], "iid")
            : LongValue(payload["merge_request"], "iid");
        if (iid <= 0)
        {
            return null;
        }

        var project = payload["project"];
        var pathWithNamespace = TextValue(project, "path_with_namespace");
        var namespaceName = TextValue(project, "namespace");
        if (string.IsNullOrWhiteSpace(pathWithNamespace))
        {
            return null;
        }

        var note = TextValue(attrs, "note");
        var username = TextValue(payload["user"], "username");
        if (note is null || username is null)
        {
            return null;
        }

        var peerId = pathWithNamespace + "#" + iid + ":" + noteableType;
        var metadata = new Dictionary<string, object>
        {
            ["peer"] = peerId,
            ["senderId"] = username,
        };
        if (namespaceName is not null)
        {
            metadata["accountId"] = namespaceName;
        }
        return new InboundMessage(username, note, _channelId, metadata);
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

    private static bool BoolValue(JsonNode? node, string field)
    {
        var v = node?[field];
        if (v is null)
        {
            return false;
        }
        return v.GetValueKind() == JsonValueKind.True;
    }
}
