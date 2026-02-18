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
using AgentScope.Core.Model;

namespace AgentScope.Core.Formatter.DashScope;

/// <summary>
/// Parses DashScope API responses to AgentScope ChatResponse.
/// DashScope 响应解析器
/// 
/// Java参考: io.agentscope.core.formatter.dashscope.DashScopeResponseParser
/// </summary>
public static class DashScopeResponseParser
{
    /// <summary>
    /// Placeholder name for tool call argument fragments in streaming responses.
    /// </summary>
    private const string FragmentPlaceholder = "__fragment__";

    /// <summary>
    /// Parse DashScopeResponse to AgentScope ChatResponse.
    /// </summary>
    /// <param name="response">DashScope response DTO</param>
    /// <param name="startTime">Request start time for calculating duration</param>
    /// <returns>AgentScope ChatResponse</returns>
    public static ChatResponse ParseResponse(DashScopeResponse response, DateTime startTime)
    {
        try
        {
            var contentBlocks = new List<Message.ContentBlock>();
            string? finishReason = null;
            string? reasoningContent = null;
            string? textContent = null;
            var toolCalls = new List<ToolCallInfo>();

            if (response.Output != null)
            {
                var choice = response.Output.FirstChoice;
                if (choice?.Message != null)
                {
                    var message = choice.Message;

                    // Order matters! Follow this processing order:
                    // 1. ThinkingBlock first (reasoning_content)
                    // 2. Then TextBlock (content)
                    // 3. Finally ToolUseBlock (tool_calls)

                    reasoningContent = message.ReasoningContent;

                    // Get text content
                    textContent = message.ContentAsString;
                    if (string.IsNullOrEmpty(textContent) && message.ContentAsList?.Count > 0)
                    {
                        // Extract text from multimodal content
                        var firstText = message.ContentAsList
                            .FirstOrDefault(c => !string.IsNullOrEmpty(c.Text));
                        textContent = firstText?.Text;
                    }

                    // Handle tool calls
                    if (message.ToolCalls?.Count > 0)
                    {
                        foreach (var toolCall in message.ToolCalls)
                        {
                            if (toolCall?.Function == null) continue;

                            var func = toolCall.Function;
                            var id = toolCall.Id ?? $"tool_call_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                            var name = func.Name ?? "";
                            var args = func.Arguments ?? "{}";

                            if (!string.IsNullOrEmpty(name))
                            {
                                toolCalls.Add(new ToolCallInfo
                                {
                                    Id = id,
                                    Name = name,
                                    Arguments = args,
                                    Type = "function"
                                });
                            }
                            else if (!string.IsNullOrEmpty(args))
                            {
                                // Subsequent chunks with only argument fragments
                                toolCalls.Add(new ToolCallInfo
                                {
                                    Id = id,
                                    Name = FragmentPlaceholder,
                                    Arguments = args,
                                    Type = "function"
                                });
                            }
                        }
                    }

                    finishReason = choice.FinishReason;
                }

                // Fallback to output-level finish reason
                if (string.IsNullOrEmpty(finishReason))
                {
                    finishReason = response.Output.FinishReason;
                }
            }

            // Build usage info
            ChatUsage? usage = null;
            if (response.Usage != null)
            {
                usage = new ChatUsage
                {
                    InputTokens = response.Usage.InputTokens ?? 0,
                    OutputTokens = response.Usage.OutputTokens ?? 0,
                    TotalTokens = response.Usage.TotalTokens ?? 0,
                    TimeSeconds = (DateTime.UtcNow - startTime).TotalSeconds
                };
            }

            var chatResponse = new ChatResponse
            {
                Id = response.RequestId ?? "",
                Text = textContent ?? "",
                Content = textContent ?? "",
                ToolCalls = toolCalls.Count > 0 ? toolCalls : null,
                Usage = usage,
                StopReason = finishReason
            };

            // Add thinking content to metadata if present
            if (!string.IsNullOrEmpty(reasoningContent))
            {
                chatResponse.Metadata ??= new Dictionary<string, object>();
                chatResponse.Metadata["thinking"] = reasoningContent;
            }

            return chatResponse;
        }
        catch (System.Exception ex)
        {
            throw new FormatterException($"Failed to parse DashScope response: {ex.Message}");
        }
    }
}
