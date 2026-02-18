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
using System.Linq;
using AgentScope.Core.Formatter.Anthropic.Dto;
using AgentScope.Core.Message;

namespace AgentScope.Core.Formatter.Anthropic;

/// <summary>
/// Converts AgentScope Msg objects to Anthropic SDK MessageParam types.
/// Anthropic 消息转换器
/// 
/// Java参考: io.agentscope.core.formatter.anthropic.AnthropicMessageConverter
/// </summary>
public static class AnthropicMessageConverter
{
    /// <summary>
    /// Convert list of Msg to list of Anthropic Message.
    /// 将 AgentScope 消息列表转换为 Anthropic 消息列表
    /// 
    /// Important: Anthropic API has special requirements:
    /// - Only the first message can be a system message (handled via system parameter)
    /// - Tool results must be in separate user messages
    /// </summary>
    /// <param name="messages">AgentScope messages</param>
    /// <returns>Anthropic messages</returns>
    public static List<AnthropicMessage> Convert(List<Msg> messages)
    {
        var result = new List<AnthropicMessage>();

        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            bool isFirstMessage = (i == 0);

            // Check if this is a tool message (metadata contains tool_call_id)
            if (IsToolResultMessage(msg))
            {
                // Tool results are handled separately as user messages
                var toolResultMsg = ConvertToolResultMessage(msg);
                if (toolResultMsg != null)
                {
                    result.Add(toolResultMsg);
                }
            }
            else
            {
                var anthropicMsg = ConvertMessage(msg, isFirstMessage);
                if (anthropicMsg != null)
                {
                    result.Add(anthropicMsg);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Extract system message content if present in the first message.
    /// 从第一条消息中提取系统消息内容
    /// 
    /// In Anthropic API, system messages are passed via a separate 'system' parameter,
    /// not as part of the messages list.
    /// </summary>
    /// <param name="messages">All messages including potential system message</param>
    /// <returns>System message content or null</returns>
    public static List<AnthropicSystemMessage>? ExtractSystemMessage(List<Msg> messages)
    {
        if (messages.Count == 0)
        {
            return null;
        }

        var first = messages[0];
        if (first.Role == "system")
        {
            var text = first.GetTextContent();
            if (!string.IsNullOrEmpty(text))
            {
                return new List<AnthropicSystemMessage>
                {
                    new AnthropicSystemMessage { Text = text }
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Convert a single AgentScope message to Anthropic message.
    /// </summary>
    private static AnthropicMessage? ConvertMessage(Msg msg, bool isFirstMessage)
    {
        var role = ConvertRole(msg.Role, isFirstMessage);
        var contentBlocks = new List<AnthropicContentBlock>();

        // Handle text content
        var textContent = msg.GetTextContent();
        if (!string.IsNullOrEmpty(textContent))
        {
            contentBlocks.Add(new Dto.TextBlock { Text = textContent });
        }

        // Handle tool calls (from metadata)
        if (msg.Metadata?.TryGetValue("tool_calls", out var toolCalls) == true && toolCalls is List<object> calls)
        {
            foreach (var call in calls)
            {
                if (call is Dictionary<string, object> toolCall)
                {
                    var toolUseBlock = ConvertToolCall(toolCall);
                    if (toolUseBlock != null)
                    {
                        contentBlocks.Add(toolUseBlock);
                    }
                }
            }
        }

        // Handle images (from metadata)
        if (msg.Metadata?.TryGetValue("image_urls", out var imageUrls) == true && imageUrls is List<string> urls)
        {
            foreach (var url in urls)
            {
                var imageBlock = ConvertImage(url);
                if (imageBlock != null)
                {
                    contentBlocks.Add(imageBlock);
                }
            }
        }

        if (contentBlocks.Count == 0)
        {
            return null;
        }

        return new AnthropicMessage
        {
            Role = role,
            Content = contentBlocks
        };
    }

    /// <summary>
    /// Check if a message is a tool result message.
    /// </summary>
    private static bool IsToolResultMessage(Msg msg)
    {
        return msg.Role == "tool" || 
               (msg.Metadata?.ContainsKey("tool_call_id") == true);
    }

    /// <summary>
    /// Convert a tool result message to Anthropic user message.
    /// </summary>
    private static AnthropicMessage? ConvertToolResultMessage(Msg msg)
    {
        var toolCallId = msg.Metadata?["tool_call_id"] as string;
        if (string.IsNullOrEmpty(toolCallId))
        {
            return null;
        }

        var content = msg.GetTextContent() ?? "";
        var isError = msg.Metadata?.TryGetValue("is_error", out var errorVal) == true && 
                      errorVal is true;

        var toolResultContent = new List<Dto.ToolResultContent>
        {
            new Dto.ToolResultContent
            {
                Type = "text",
                Text = content
            }
        };

        var toolResultBlock = new Dto.ToolResultBlock
        {
            ToolUseId = toolCallId,
            Content = toolResultContent,
            IsError = isError ? true : null
        };

        return new AnthropicMessage
        {
            Role = AnthropicRole.User,
            Content = new List<AnthropicContentBlock> { toolResultBlock }
        };
    }

    /// <summary>
    /// Convert a tool call from metadata to Anthropic ToolUseBlock.
    /// </summary>
    private static Dto.ToolUseBlock? ConvertToolCall(Dictionary<string, object> toolCall)
    {
        var id = toolCall.GetValueOrDefault("id") as string;
        var name = toolCall.GetValueOrDefault("name") as string ?? 
                   toolCall.GetValueOrDefault("function")?.ToString();
        var arguments = toolCall.GetValueOrDefault("arguments") as Dictionary<string, object> ??
                        new Dictionary<string, object>();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
        {
            return null;
        }

        return new Dto.ToolUseBlock
        {
            Id = id,
            Name = name,
            Input = arguments
        };
    }

    /// <summary>
    /// Convert an image URL to Anthropic ImageBlock.
    /// </summary>
    private static Dto.ImageBlock? ConvertImage(string url)
    {
        try
        {
            // Handle base64 data URLs
            if (url.StartsWith("data:image/"))
            {
                var parts = url.Split(',');
                if (parts.Length == 2)
                {
                    var mediaType = parts[0].Split(';')[0].Replace("data:", "");
                    var base64Data = parts[1];

                    return new Dto.ImageBlock
                    {
                        Source = new Dto.ImageSource
                        {
                            Type = "base64",
                            MediaType = mediaType,
                            Data = base64Data
                        }
                    };
                }
            }

            // For HTTP URLs, we would need to download and convert to base64
            // For now, return null as this requires async I/O
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Convert AgentScope role to Anthropic role.
    /// 
    /// Important: Anthropic only allows the first message to be system.
    /// Non-first system messages are converted to user.
    /// Tool results are always user messages.
    /// </summary>
    private static AnthropicRole ConvertRole(string role, bool isFirstMessage)
    {
        return role.ToLower() switch
        {
            "system" => AnthropicRole.User, // Anthropic uses user for system messages in the messages list
            "user" => AnthropicRole.User,
            "assistant" => AnthropicRole.Assistant,
            "tool" => AnthropicRole.User, // Tool results are user messages
            _ => AnthropicRole.User
        };
    }
}
