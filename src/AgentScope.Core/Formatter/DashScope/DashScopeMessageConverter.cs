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
using AgentScope.Core.Formatter.DashScope.Dto;
using AgentScope.Core.Message;

namespace AgentScope.Core.Formatter.DashScope;

/// <summary>
/// Converts AgentScope Msg objects to DashScope DTO message types.
/// DashScope 消息转换器
/// 
/// Java参考: io.agentscope.core.formatter.dashscope.DashScopeMessageConverter
/// </summary>
public static class DashScopeMessageConverter
{
    /// <summary>
    /// Convert list of Msg to list of DashScopeMessage.
    /// </summary>
    /// <param name="messages">AgentScope messages</param>
    /// <param name="useMultimodalFormat">Whether to use multimodal content format</param>
    /// <returns>List of DashScopeMessage</returns>
    public static List<DashScopeMessage> Convert(List<Msg> messages, bool useMultimodalFormat = false)
    {
        var result = new List<DashScopeMessage>();

        foreach (var msg in messages)
        {
            var hasMedia = HasMediaContent(msg);
            var dsMsg = ConvertToMessage(msg, useMultimodalFormat || hasMedia);
            if (dsMsg != null)
            {
                result.Add(dsMsg);
            }
        }

        return result;
    }

    /// <summary>
    /// Convert single Msg to DashScopeMessage.
    /// </summary>
    private static DashScopeMessage? ConvertToMessage(Msg msg, bool useMultimodalFormat)
    {
        if (useMultimodalFormat)
        {
            return ConvertToMultimodalContent(msg);
        }
        else
        {
            return ConvertToSimpleContent(msg);
        }
    }

    /// <summary>
    /// Convert message to multimodal format with List of content parts.
    /// </summary>
    private static DashScopeMessage ConvertToMultimodalContent(Msg msg)
    {
        // Special handling for TOOL role messages
        if (msg.Role == "tool")
        {
            return ConvertToolRoleMessage(msg);
        }

        var contents = new List<DashScopeContentPart>();
        var textContent = msg.GetTextContent();

        // Add text content if present
        if (!string.IsNullOrEmpty(textContent))
        {
            contents.Add(DashScopeContentPart.FromText(textContent));
        }

        // Handle images from metadata
        if (msg.Metadata?.TryGetValue("image_urls", out var imageUrls) == true && 
            imageUrls is List<string> urls)
        {
            foreach (var url in urls)
            {
                contents.Add(DashScopeContentPart.FromImage(url));
            }
        }

        // Handle tool calls from metadata for assistant messages
        var toolCalls = new List<DashScopeToolCall>();
        if (msg.Role == "assistant" && 
            msg.Metadata?.TryGetValue("tool_calls", out var tcObj) == true &&
            tcObj is List<object> tcList)
        {
            foreach (var tc in tcList)
            {
                if (tc is Dictionary<string, object> toolCall)
                {
                    var id = toolCall.GetValueOrDefault("id") as string ?? $"call_{Guid.NewGuid():N}";
                    var name = toolCall.GetValueOrDefault("name") as string ?? 
                               toolCall.GetValueOrDefault("function")?.ToString() ?? "unknown";
                    var args = toolCall.GetValueOrDefault("arguments")?.ToString() ?? "{}";

                    toolCalls.Add(new DashScopeToolCall
                    {
                        Id = id,
                        Function = new DashScopeFunction
                        {
                            Name = name,
                            Arguments = args
                        }
                    });
                }
            }
        }

        // Ensure non-empty content (required by some VL APIs)
        if (contents.Count == 0)
        {
            contents.Add(DashScopeContentPart.FromText(""));
        }

        var message = new DashScopeMessage
        {
            Role = msg.Role.ToLower(),
            Content = contents
        };

        if (toolCalls.Count > 0)
        {
            message.ToolCalls = toolCalls;
        }

        return message;
    }

    /// <summary>
    /// Convert TOOL role message to DashScopeMessage.
    /// </summary>
    private static DashScopeMessage ConvertToolRoleMessage(Msg msg)
    {
        var toolCallId = msg.Metadata?["tool_call_id"] as string;
        var name = msg.Metadata?["tool_name"] as string ?? "tool";
        var content = msg.GetTextContent() ?? "";

        var contents = new List<DashScopeContentPart>
        {
            DashScopeContentPart.FromText(content)
        };

        return new DashScopeMessage
        {
            Role = "tool",
            Content = contents,
            ToolCallId = toolCallId,
            Name = name
        };
    }

    /// <summary>
    /// Convert message to simple text format.
    /// </summary>
    private static DashScopeMessage ConvertToSimpleContent(Msg msg)
    {
        // Check if message is a tool result
        if (msg.Role == "tool" || msg.Metadata?.ContainsKey("tool_call_id") == true)
        {
            var toolCallId = msg.Metadata?["tool_call_id"] as string;
            var name = msg.Metadata?["tool_name"] as string ?? "tool";
            var content = msg.GetTextContent() ?? "";

            return new DashScopeMessage
            {
                Role = "tool",
                Content = content,
                ToolCallId = toolCallId,
                Name = name
            };
        }

        var textContent = msg.GetTextContent() ?? "";
        var message = new DashScopeMessage
        {
            Role = msg.Role.ToLower(),
            Content = textContent
        };

        // Handle tool calls for assistant messages
        if (msg.Role == "assistant" && 
            msg.Metadata?.TryGetValue("tool_calls", out var tcObj) == true &&
            tcObj is List<object> tcList && tcList.Count > 0)
        {
            var toolCalls = new List<DashScopeToolCall>();
            foreach (var tc in tcList)
            {
                if (tc is Dictionary<string, object> toolCall)
                {
                    var id = toolCall.GetValueOrDefault("id") as string ?? $"call_{Guid.NewGuid():N}";
                    var name = toolCall.GetValueOrDefault("name") as string ?? 
                               toolCall.GetValueOrDefault("function")?.ToString() ?? "unknown";
                    var args = toolCall.GetValueOrDefault("arguments")?.ToString() ?? "{}";

                    toolCalls.Add(new DashScopeToolCall
                    {
                        Id = id,
                        Function = new DashScopeFunction
                        {
                            Name = name,
                            Arguments = args
                        }
                    });
                }
            }

            if (toolCalls.Count > 0)
            {
                message.ToolCalls = toolCalls;
                // If there's text content and tool calls, clear content if empty
                if (string.IsNullOrEmpty(textContent))
                {
                    message.Content = null!;
                }
            }
        }

        return message;
    }

    /// <summary>
    /// Check if message has media content (images, audio, video).
    /// </summary>
    private static bool HasMediaContent(Msg msg)
    {
        if (msg.Url?.Count > 0)
        {
            return true;
        }

        if (msg.Metadata?.ContainsKey("image_urls") == true ||
            msg.Metadata?.ContainsKey("audio_urls") == true ||
            msg.Metadata?.ContainsKey("video_urls") == true)
        {
            return true;
        }

        return false;
    }
}
