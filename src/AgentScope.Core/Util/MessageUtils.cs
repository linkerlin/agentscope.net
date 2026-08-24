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

using System.Collections.Generic;
using System.Text;
using AgentScope.Core.Message;

namespace AgentScope.Core.Util;

/// <summary>
/// 消息工具类：消息列表摘要、角色过滤等。
/// 对应 Java: io.agentscope.core.util.MessageUtils
/// </summary>
public static class MessageUtils
{
    /// <summary>提取消息列表的文本内容拼接（复用 Msg.GetTextContent，兼容 string/ContentBlock 列表）。</summary>
    public static string ExtractText(IEnumerable<Msg>? messages)
    {
        if (messages == null) return "";
        var sb = new StringBuilder();
        foreach (var msg in messages)
        {
            var text = msg.GetTextContent();
            if (!string.IsNullOrEmpty(text))
            {
                sb.Append(text).Append('\n');
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>按角色过滤消息（role 可传 MsgRole 枚举或字符串）。</summary>
    public static List<Msg> FilterByRole(IEnumerable<Msg> messages, MsgRole role)
    {
        var roleName = role.ToString();
        return FilterByRole(messages, roleName);
    }

    /// <summary>按角色名字符串过滤消息。</summary>
    public static List<Msg> FilterByRole(IEnumerable<Msg> messages, string role)
    {
        var result = new List<Msg>();
        foreach (var m in messages)
        {
            if (string.Equals(m.Role, role, System.StringComparison.OrdinalIgnoreCase))
            {
                result.Add(m);
            }
        }

        return result;
    }

    /// <summary>统计消息列表中文本 token 的粗略估计（按 4 字符/token）。</summary>
    public static int EstimateTokens(IEnumerable<Msg>? messages)
    {
        var text = ExtractText(messages);
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Length / 4;
    }
}
