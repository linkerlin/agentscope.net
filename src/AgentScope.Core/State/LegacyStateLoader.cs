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

namespace AgentScope.Core.State;

/// <summary>
/// 旧版状态兼容加载器：尽力从历史格式/历史字段迁移到当前 AgentState，加载失败返回 null。
/// 对应 Java: io.agentscope.core.state.LegacyStateLoader
/// </summary>
public static class LegacyStateLoader
{
    /// <summary>
    /// 尝试从任意 JSON 文本加载 AgentState。兼容旧字段名（如 messages/iter/summary/reply_id）。
    /// </summary>
    public static AgentState? TryLoad(string? json, string sessionId, string? userId = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var state = new AgentState(sessionId, userId);

            if (root.TryGetProperty("summary", out var summary) || root.TryGetProperty("Summary", out summary))
            {
                state.Summary = summary.GetString() ?? "";
            }

            if (root.TryGetProperty("cur_iter", out var iter) || root.TryGetProperty("CurIter", out iter) ||
                root.TryGetProperty("curIter", out iter))
            {
                if (iter.TryGetInt32(out var n))
                {
                    state.CurIter = n;
                }
            }

            if (root.TryGetProperty("reply_id", out var rid) || root.TryGetProperty("ReplyId", out rid) ||
                root.TryGetProperty("replyId", out rid))
            {
                state.ReplyId = rid.GetString() ?? "";
            }

            return state;
        }
        catch
        {
            return null;
        }
    }
}
