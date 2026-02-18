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
using System.Text.Json;
using AgentScope.Core.Formatter.Anthropic.Dto;
using AgentScope.Core.Message;
using AgentScope.Core.Model;

namespace AgentScope.Core.Formatter.Anthropic;

/// <summary>
/// Parses Anthropic API responses into AgentScope ChatResponse objects.
/// Anthropic 响应解析器
/// 
/// Java参考: io.agentscope.core.formatter.anthropic.AnthropicResponseParser
/// </summary>
public static class AnthropicResponseParser
{
    /// <summary>
    /// Parse non-streaming Anthropic response to ChatResponse.
    /// </summary>
    /// <param name="response">Anthropic response</param>
    /// <param name="startTime">Request start time</param>
    /// <returns>AgentScope ChatResponse</returns>
    public static ChatResponse ParseMessage(AnthropicResponse response, DateTime startTime)
    {
        var contentBlocks = new List<ContentBlock>();
        var toolCalls = new List<ToolCallInfo>();
        string? thinkingContent = null;

        // Process content blocks
        foreach (var block in response.Content)
        {
            switch (block)
            {
                case Dto.TextBlock textBlock:
                    contentBlocks.Add(new Message.TextBlock { Text = textBlock.Text });
                    break;

                case Dto.ThinkingBlock thinkingBlock:
                    thinkingContent = thinkingContent != null 
                        ? thinkingContent + "\n" + thinkingBlock.Thinking 
                        : thinkingBlock.Thinking;
                    break;

                case Dto.ToolUseBlock toolUse:
                    toolCalls.Add(new ToolCallInfo
                    {
                        Id = toolUse.Id,
                        Name = toolUse.Name,
                        Arguments = JsonSerializer.Serialize(toolUse.Input),
                        Type = "function"
                    });
                    break;
            }
        }

        // Build response text from content blocks
        var responseText = string.Join("\n", contentBlocks.OfType<Message.TextBlock>().Select(t => t.Text));

        // Build usage info
        var usage = new ChatUsage
        {
            InputTokens = response.Usage.InputTokens,
            OutputTokens = response.Usage.OutputTokens,
            TotalTokens = response.Usage.InputTokens + response.Usage.OutputTokens,
            TimeSeconds = (DateTime.UtcNow - startTime).TotalSeconds
        };

        var chatResponse = new ChatResponse
        {
            Id = response.Id,
            Text = responseText,  // Set both Text (base class) and Content
            Content = responseText,
            ToolCalls = toolCalls.Count > 0 ? toolCalls : null,
            Usage = usage,
            Model = response.Model,
            StopReason = response.StopReason
        };

        // Add thinking content to metadata if present
        if (!string.IsNullOrEmpty(thinkingContent))
        {
            chatResponse.Metadata ??= new Dictionary<string, object>();
            chatResponse.Metadata["thinking"] = thinkingContent;
        }

        return chatResponse;
    }

    /// <summary>
    /// Parse streaming Anthropic event to partial ChatResponse.
    /// </summary>
    /// <param name="streamEvent">Stream event</param>
    /// <param name="messageId">Current message ID</param>
    /// <param name="accumulatedToolInput">Accumulated tool input for streaming tool calls</param>
    /// <returns>Partial ChatResponse or null</returns>
    public static ChatResponse? ParseStreamEvent(
        AnthropicStreamEvent streamEvent, 
        ref string? messageId,
        ref Dictionary<string, StringBuilder> accumulatedToolInput)
    {
        // Message start - capture ID
        if (streamEvent.EventType == "message_start" && streamEvent.Message != null)
        {
            messageId = streamEvent.Message.Id;
            return null;
        }

        // Content block delta - text or tool input
        if (streamEvent.EventType == "content_block_delta" && streamEvent.Delta != null)
        {
            // Text delta
            if (!string.IsNullOrEmpty(streamEvent.Delta.Text))
            {
                return new ChatResponse
                {
                    Id = messageId ?? "",
                    Content = streamEvent.Delta.Text,
                    ToolCalls = null,
                    Usage = null
                };
            }

            // Thinking delta
            if (!string.IsNullOrEmpty(streamEvent.Delta.Thinking))
            {
                return new ChatResponse
                {
                    Id = messageId ?? "",
                    Content = "",
                    Metadata = new Dictionary<string, object> { ["thinking"] = streamEvent.Delta.Thinking },
                    Usage = null
                };
            }

            // Tool input delta (streaming JSON)
            if (!string.IsNullOrEmpty(streamEvent.Delta.PartialJson))
            {
                // Accumulate partial JSON for tool calls
                // The actual tool call will be assembled when content_block_stop event fires
                return null;
            }
        }

        // Content block start - tool use start
        if (streamEvent.EventType == "content_block_start" && streamEvent.ContentBlock is Dto.ToolUseBlock toolUse)
        {
            // Initialize accumulation for this tool call
            accumulatedToolInput[toolUse.Id] = new StringBuilder();
            
            return new ChatResponse
            {
                Id = messageId ?? "",
                Content = "",
                ToolCalls = new List<ToolCallInfo>
                {
                    new ToolCallInfo
                    {
                        Id = toolUse.Id,
                        Name = toolUse.Name,
                        Arguments = "", // Will be filled on content_block_stop
                        Type = "function"
                    }
                },
                Usage = null
            };
        }

        // Message delta - usage info
        if (streamEvent.EventType == "message_delta" && streamEvent.Usage != null)
        {
            return new ChatResponse
            {
                Id = messageId ?? "",
                Content = "",
                Usage = new ChatUsage
                {
                    OutputTokens = streamEvent.Usage.OutputTokens
                }
            };
        }

        // Message stop - final event
        if (streamEvent.EventType == "message_stop")
        {
            return new ChatResponse
            {
                Id = messageId ?? "",
                Content = "",
                IsComplete = true
            };
        }

        return null;
    }

    /// <summary>
    /// Parse JSON input for tool calls.
    /// </summary>
    private static Dictionary<string, object> ParseJsonInput(string json, string toolName)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new Dictionary<string, object>();
        }

        try
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return result ?? new Dictionary<string, object>();
        }
        catch (System.Exception ex)
        {
            // Log warning but don't fail
            Console.WriteLine($"Warning: Failed to parse tool input JSON for tool {toolName}: {ex.Message}");
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// StringBuilder class for accumulating tool input in streams.
    /// </summary>
    public class StringBuilder
    {
        private readonly System.Text.StringBuilder _sb = new();

        public void Append(string text) => _sb.Append(text);
        public override string ToString() => _sb.ToString();
    }
}
