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
using AgentScope.Core.Formatter.Anthropic.Dto;
using AgentScope.Core.Message;
using GenerateOptions = AgentScope.Core.Formatter.GenerateOptions;

namespace AgentScope.Core.Formatter.Anthropic;

/// <summary>
/// 多 Agent 对话历史的 Anthropic formatter：把消息的 Name 折叠进内容，
/// 在单角色（user/assistant）API 中保留多 Agent 角色归属。
/// 对应 Java: io.agentscope.extensions.model.anthropic.formatter.AnthropicMultiAgentFormatter
/// </summary>
public class AnthropicMultiAgentFormatter : AnthropicBaseFormatter
{
    public AnthropicMultiAgentFormatter() { }

    public override AnthropicRequest Format(List<Msg> messages, GenerateOptions? options = null)
        => base.Format(PrefixAgentNames(messages), options);

    /// <summary>
    /// 为带 Name 的消息在文本前折叠 【Name】 前缀。无 Name 的消息原样透传。
    /// </summary>
    private static List<Msg> PrefixAgentNames(List<Msg> messages)
    {
        var result = new List<Msg>(messages.Count);
        foreach (var msg in messages)
        {
            var name = msg.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                result.Add(msg);
                continue;
            }

            var text = msg.GetTextContent() ?? "";
            result.Add(Msg.Builder()
                .Name(msg.Name)
                .Role(msg.Role)
                .TextContent($"\u3010{name}\u3011 " + text)
                .Build());
        }

        return result;
    }
}
