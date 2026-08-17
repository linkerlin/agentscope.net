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

using AgentScope.Core.AgUI.Model;
using AgentScope.Core.Message;

namespace AgentScope.Core.AgUI.Converter;

/// <summary>
/// Bidirectional converter between AG-UI and AgentScope message formats.
/// AG-UI ↔ AgentScope 消息双向转换器。
/// Corresponds to Java: AguiMessageConverter
/// </summary>
public sealed class AguiMessageConverter
{
    /// <summary>
    /// Converts an AG-UI message to an AgentScope <see cref="Msg"/>.
    /// AG-UI 消息 → AgentScope.Msg 转换。
    /// </summary>
    /// <param name="aguiMsg">The AG-UI message to convert / 要转换的 AG-UI 消息</param>
    /// <returns>The converted AgentScope message / 转换后的 AgentScope 消息</returns>
    public Msg ToMsg(AguiMessage aguiMsg)
    {
        var builder = Msg.Builder()
            .Role(aguiMsg.Role)
            .Name(aguiMsg.Role);

        // 处理纯文本内容
        // Handle plain text content
        if (aguiMsg.Text != null)
            builder.TextContent(aguiMsg.Text);

        // 处理多模态内容块（文本、图片等）
        // Handle multimodal content blocks (text, image, etc.)
        if (aguiMsg.Blocks != null)
        {
            foreach (var block in aguiMsg.Blocks)
            {
                switch (block)
                {
                    case TextInputContent t:
                        builder.Content(new TextBlock { Text = t.Text });
                        break;
                    case ImageInputContent img:
                        builder.Content(new ImageBlock
                        {
                            Url = img.Source is UrlInputSource url ? url.Url : "",
                            Data = img.Source is DataInputSource data ? Convert.FromBase64String(data.Base64) : null
                        });
                        break;
                }
            }
        }

        // 处理工具调用内容
        // Handle tool call content
        if (aguiMsg.ToolCalls != null)
        {
            foreach (var tc in aguiMsg.ToolCalls)
            {
                builder.Content(new ToolUseBlock
                {
                    Id = tc.Id,
                    Name = tc.Function?.Name ?? "unknown",
                    Input = tc.Function?.Arguments != null
                        ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(tc.Function.Arguments)
                        : null
                });
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// Converts an AgentScope <see cref="Msg"/> to an AG-UI message.
    /// AgentScope.Msg → AG-UI 消息转换。
    /// </summary>
    /// <param name="msg">The AgentScope message to convert / 要转换的 AgentScope 消息</param>
    /// <returns>The converted AG-UI message / 转换后的 AG-UI 消息</returns>
    public AguiMessage ToAguiMessage(Msg msg)
    {
        return AguiMessage.AssistantMessage(msg.GetTextContent() ?? "");
    }

    /// <summary>
    /// Converts all messages in a <see cref="RunAgentInput"/> to a list of AgentScope <see cref="Msg"/>.
    /// 批量转换 RunAgentInput 中的所有消息为 AgentScope Msg 列表。
    /// </summary>
    /// <param name="input">The run input containing AG-UI messages / 包含 AG-UI 消息的运行输入</param>
    /// <returns>A list of AgentScope messages / AgentScope 消息列表</returns>
    public List<Msg> ToMsgList(RunAgentInput input)
    {
        var msgs = new List<Msg>();
        foreach (var aguiMsg in input.Messages)
            msgs.Add(ToMsg(aguiMsg));
        return msgs;
    }
}
