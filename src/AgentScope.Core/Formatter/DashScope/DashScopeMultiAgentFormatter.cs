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

using System;
using System.Collections.Generic;
using AgentScope.Core.Formatter.DashScope.Dto;
using AgentScope.Core.Message;
using GenerateOptions = AgentScope.Core.Formatter.GenerateOptions;

namespace AgentScope.Core.Formatter.DashScope;

/// <summary>
/// 多 Agent 对话历史的 DashScope formatter：把消息的 Name 折叠进内容。
/// 对应 Java: io.agentscope.extensions.model.dashscope.formatter.DashScopeMultiAgentFormatter
/// </summary>
public class DashScopeMultiAgentFormatter
{
    private readonly string _modelName;

    public DashScopeMultiAgentFormatter(string modelName = "qwen-plus")
    {
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
    }

    public DashScopeRequest Format(List<Msg> messages, GenerateOptions? options = null, bool stream = false)
    {
        var model = ResolveModel(options);
        var converted = DashScopeMessageConverter.Convert(PrefixAgentNames(messages), useMultimodalFormat: true);
        return new DashScopeChatFormatter(_modelName).BuildRequest(model, converted, stream, options, null, null, null);
    }

    private string ResolveModel(GenerateOptions? options)
    {
        if (options?.AdditionalBodyParams?.TryGetValue("model", out var m) == true && m is string ms)
        {
            return ms;
        }

        return _modelName;
    }

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
