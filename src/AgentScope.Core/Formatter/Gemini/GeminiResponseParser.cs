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
using System.Text.Json;
using AgentScope.Core.Formatter.Gemini.Dto;
using AgentScope.Core.Model;

namespace AgentScope.Core.Formatter.Gemini;

/// <summary>
/// Gemini 响应解析器
/// Gemini response parser
/// 
/// 将Gemini API响应转换为AgentScope ModelResponse
/// Converts Gemini API response to AgentScope ModelResponse
/// </summary>
public static class GeminiResponseParser
{
    /// <summary>
    /// 解析Gemini响应为ModelResponse
    /// Parse Gemini response to ModelResponse
    /// </summary>
    /// <param name="response">Gemini响应 / Gemini response</param>
    /// <param name="startTime">请求开始时间 / Request start time</param>
    /// <returns>AgentScope ModelResponse</returns>
    public static ModelResponse ParseResponse(GeminiResponse response, DateTime startTime)
    {
        if (response?.Candidates == null || response.Candidates.Count == 0)
        {
            return new ModelResponse
            {
                Success = false,
                Text = "",
                Error = "No candidates in response"
            };
        }

        var candidate = response.Candidates[0];
        var content = candidate.Content;

        if (content?.Parts == null || content.Parts.Count == 0)
        {
            return new ModelResponse
            {
                Success = false,
                Text = "",
                Error = "No content in candidate"
            };
        }

        // 提取文本内容
        // Extract text content
        var text = ExtractTextFromParts(content.Parts);

        // 检查是否有函数调用
        // Check for function calls
        var toolCalls = ExtractToolCallsFromParts(content.Parts);

        // 检查完成原因
        // Check finish reason
        var finishReason = candidate.FinishReason;
        var isComplete = finishReason == null || 
                         finishReason.Equals("STOP", StringComparison.OrdinalIgnoreCase);

        // 如果有工具调用，返回 ChatResponse 以包含工具调用信息
        // If there are tool calls, return ChatResponse to include tool call info
        if (toolCalls.Count > 0)
        {
            return new ChatResponse
            {
                Success = isComplete,
                Text = text,
                Error = isComplete ? null : $"Finish reason: {finishReason}",
                ToolCalls = toolCalls,
                Model = response.ModelVersion,
                StopReason = finishReason,
                Metadata = new Dictionary<string, object>
                {
                    ["modelVersion"] = response.ModelVersion ?? "",
                    ["finishReason"] = finishReason ?? "",
                    ["latencyMs"] = (DateTime.UtcNow - startTime).TotalMilliseconds
                }
            };
        }

        return new ModelResponse
        {
            Success = isComplete,
            Text = text,
            Error = isComplete ? null : $"Finish reason: {finishReason}",
            Metadata = new Dictionary<string, object>
            {
                ["modelVersion"] = response.ModelVersion ?? "",
                ["finishReason"] = finishReason ?? "",
                ["latencyMs"] = (DateTime.UtcNow - startTime).TotalMilliseconds
            }
        };
    }

    /// <summary>
    /// 从Parts提取文本
    /// Extract text from parts
    /// </summary>
    private static string ExtractTextFromParts(List<GeminiPart> parts)
    {
        var texts = new List<string>();
        foreach (var part in parts)
        {
            if (!string.IsNullOrEmpty(part.Text))
            {
                texts.Add(part.Text);
            }
        }
        return string.Join("\n", texts);
    }

    /// <summary>
    /// 从Parts提取工具调用
    /// Extract tool calls from parts
    /// </summary>
    private static List<ToolCallInfo> ExtractToolCallsFromParts(List<GeminiPart> parts)
    {
        var toolCalls = new List<ToolCallInfo>();
        foreach (var part in parts)
        {
            if (part.FunctionCall != null)
            {
                toolCalls.Add(new ToolCallInfo
                {
                    Id = Guid.NewGuid().ToString("N")[..8], // Gemini doesn't provide call IDs
                    Name = part.FunctionCall.Name,
                    Arguments = part.FunctionCall.Args != null 
                        ? JsonSerializer.Serialize(part.FunctionCall.Args)
                        : "{}"
                });
            }
        }
        return toolCalls;
    }
}
