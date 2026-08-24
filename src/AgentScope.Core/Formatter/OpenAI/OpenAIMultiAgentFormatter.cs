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
using AgentScope.Core.Message;
using AgentScope.Core.Formatter.OpenAI.Dto;

namespace AgentScope.Core.Formatter.OpenAI;

/// <summary>
/// 多 Agent 对话历史的 OpenAI formatter：把消息的 Name 折叠进内容
/// 对应 Java: io.agentscope.core.formatter.openai.OpenAIMultiAgentFormatter
/// </summary>
public class OpenAIMultiAgentFormatter : OpenAIBaseFormatter
{
    public OpenAIMultiAgentFormatter(string modelName) : base(modelName) { }

    public override OpenAIRequest Format(List<Msg> messages, GenerateOptions? options = null)
    {
        var openAIMessages = new List<OpenAIMessage>();
        foreach (var msg in messages)
        {
            var named = !string.IsNullOrWhiteSpace(msg.Name);
            var prefix = named ? $"\u3010{msg.Name}\u3011 " : "";
            var content = prefix + (msg.GetTextContent() ?? "");

            string role = msg.Role switch
            {
                "assistant" => "assistant",
                "system" => "system",
                _ => "user"
            };

            openAIMessages.Add(new OpenAIMessage { Role = role, Content = content });
        }

        var request = new OpenAIRequest { Model = ModelName, Messages = openAIMessages };
        if (options != null) ApplyOptions(request, options);
        return request;
    }
}
