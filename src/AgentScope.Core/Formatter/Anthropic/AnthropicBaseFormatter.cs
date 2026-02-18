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
using System.Text.Json.Serialization;
using AgentScope.Core.Formatter.Anthropic.Dto;
using AgentScope.Core.Message;
using AgentScope.Core.Model;

// Use the global GenerateOptions from Formatter namespace
using GenerateOptions = AgentScope.Core.Formatter.GenerateOptions;

namespace AgentScope.Core.Formatter.Anthropic;

/// <summary>
/// Serializer options for Anthropic API.
/// </summary>
public static class AnthropicSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

/// <summary>
/// Abstract base formatter for Anthropic API with shared logic for handling Anthropic-specific requirements.
/// Anthropic 基础格式化器
/// 
/// This class handles:
/// - System message extraction and application (Anthropic requires system via system parameter)
/// - Tool choice configuration with GenerateOptions
/// 
/// Java参考: io.agentscope.core.formatter.anthropic.AnthropicBaseFormatter
/// </summary>
public abstract class AnthropicBaseFormatter
{
    /// <summary>
    /// Default max tokens for Anthropic API (required parameter)
    /// </summary>
    protected const int DefaultMaxTokens = 4096;

    /// <summary>
    /// Format messages to Anthropic request.
    /// 格式化消息为 Anthropic 请求
    /// </summary>
    /// <param name="messages">AgentScope messages</param>
    /// <param name="options">Generation options</param>
    /// <returns>Anthropic request</returns>
    public virtual AnthropicRequest Format(List<Msg> messages, GenerateOptions? options = null)
    {
        // Extract system message (Anthropic uses separate system parameter)
        var systemMessages = AnthropicMessageConverter.ExtractSystemMessage(messages);

        // Convert remaining messages
        var filteredMessages = messages;
        if (systemMessages != null && messages.Count > 0 && messages[0].Role == "system")
        {
            // Skip first system message as it's extracted to system parameter
            filteredMessages = messages.Skip(1).ToList();
        }

        var anthropicMessages = AnthropicMessageConverter.Convert(filteredMessages);

        // Build request
        var request = new AnthropicRequest
        {
            Model = GetModelName(options),
            Messages = anthropicMessages,
            System = systemMessages,
            MaxTokens = options?.MaxTokens ?? DefaultMaxTokens
        };

        // Apply generation options
        request = ApplyOptions(request, options);

        return request;
    }

    /// <summary>
    /// Parse Anthropic response to ChatResponse.
    /// 解析 Anthropic 响应
    /// </summary>
    /// <param name="response">Anthropic response</param>
    /// <param name="startTime">Request start time</param>
    /// <returns>AgentScope ChatResponse</returns>
    public virtual Model.ChatResponse Parse(AnthropicResponse response, DateTime startTime)
    {
        return AnthropicResponseParser.ParseMessage(response, startTime);
    }

    /// <summary>
    /// Parse JSON response string to ParsedResponse.
    /// 解析 JSON 响应字符串
    /// </summary>
    /// <param name="json">JSON response string</param>
    /// <returns>Parsed response</returns>
    public virtual ParsedResponse? Parse(string json)
    {
        try
        {
            var response = JsonSerializer.Deserialize<AnthropicResponse>(json, AnthropicSerializerOptions.Default);
            if (response == null) return null;

            return ParseToParsedResponse(response);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Convert AnthropicResponse to ParsedResponse.
    /// </summary>
    private ParsedResponse ParseToParsedResponse(AnthropicResponse response)
    {
        var result = new ParsedResponse
        {
            Id = response.Id,
            Model = response.Model,
            StopReason = response.StopReason,
            Usage = response.Usage != null ? new UsageInfo
            {
                InputTokens = response.Usage.InputTokens,
                OutputTokens = response.Usage.OutputTokens
            } : null
        };

        var toolCalls = new List<ToolCall>();
        var textParts = new List<string>();

        foreach (var block in response.Content)
        {
            switch (block)
            {
                case Dto.TextBlock textBlock:
                    textParts.Add(textBlock.Text);
                    break;
                case Dto.ToolUseBlock toolUse:
                    toolCalls.Add(new ToolCall
                    {
                        Id = toolUse.Id,
                        Name = toolUse.Name,
                        InputJson = JsonSerializer.Serialize(toolUse.Input)
                    });
                    break;
            }
        }

        result.TextContent = string.Join("\n", textParts);
        if (toolCalls.Count > 0)
        {
            result.ToolCalls = toolCalls;
        }

        return result;
    }

    /// <summary>
    /// Get model name from options or use default.
    /// </summary>
    protected virtual string GetModelName(GenerateOptions? options)
    {
        // Check for model in options metadata
        if (options?.AdditionalBodyParams?.TryGetValue("model", out var modelObj) == true &&
            modelObj is string modelStr)
        {
            return modelStr;
        }

        // Default to Claude 3.5 Sonnet
        return "claude-3-5-sonnet-20241022";
    }

    /// <summary>
    /// Apply generation options to Anthropic request.
    /// </summary>
    protected virtual AnthropicRequest ApplyOptions(AnthropicRequest request, GenerateOptions? options)
    {
        if (options == null)
        {
            return request;
        }

        // Temperature
        if (options.Temperature.HasValue)
        {
            request = request with { Temperature = options.Temperature.Value };
        }

        // Top P
        if (options.TopP.HasValue)
        {
            request = request with { TopP = options.TopP.Value };
        }

        // Top K
        if (options.TopK.HasValue)
        {
            request = request with { TopK = options.TopK.Value };
        }

        // Max tokens (already set in Format, but allow override)
        if (options.MaxTokens.HasValue)
        {
            request = request with { MaxTokens = options.MaxTokens.Value };
        }

        // Stop sequences
        if (options.Stop?.Count > 0)
        {
            request = request with { StopSequences = options.Stop };
        }

        // Apply tools if specified
        if (options.AdditionalBodyParams?.TryGetValue("tools", out var toolsObj) == true &&
            toolsObj is List<ToolSchema> tools)
        {
            request = ApplyTools(request, tools, options);
        }

        // Apply tool choice if specified
        if (options.AdditionalBodyParams?.TryGetValue("tool_choice", out var toolChoiceObj) == true &&
            toolChoiceObj is ToolChoice toolChoice)
        {
            request = ApplyToolChoice(request, toolChoice);
        }

        // Apply thinking config if specified (for Claude 3.7 Sonnet)
        if (options.AdditionalBodyParams?.TryGetValue("thinking", out var thinkingObj) == true &&
            thinkingObj is ThinkingConfig thinking)
        {
            request = request with { Thinking = thinking };
        }

        return request;
    }

    /// <summary>
    /// Apply tool schemas to request.
    /// </summary>
    protected virtual AnthropicRequest ApplyTools(AnthropicRequest request, List<ToolSchema> tools, GenerateOptions options)
    {
        if (tools == null || tools.Count == 0)
        {
            return request;
        }

        var anthropicTools = tools.Select(t => new AnthropicTool
        {
            Name = t.Name,
            Description = t.Description ?? $"Tool: {t.Name}",
            InputSchema = t.Parameters ?? new Dictionary<string, object>()
        }).ToList();

        return request with { Tools = anthropicTools };
    }

    /// <summary>
    /// Apply tool choice to request.
    /// </summary>
    protected virtual AnthropicRequest ApplyToolChoice(AnthropicRequest request, ToolChoice toolChoice)
    {
        AnthropicToolChoice anthropicToolChoice;

        switch (toolChoice.Type)
        {
            case ToolChoiceType.Auto:
                anthropicToolChoice = new AnthropicToolChoice { Type = AnthropicToolChoiceType.Auto };
                break;
            case ToolChoiceType.None:
                // Anthropic doesn't have None, use Any as closest equivalent
                anthropicToolChoice = new AnthropicToolChoice { Type = AnthropicToolChoiceType.Any };
                break;
            case ToolChoiceType.Required:
                // Anthropic doesn't have Required, use Any which forces tool use
                anthropicToolChoice = new AnthropicToolChoice { Type = AnthropicToolChoiceType.Any };
                break;
            case ToolChoiceType.Specific:
                anthropicToolChoice = new AnthropicToolChoice
                {
                    Type = AnthropicToolChoiceType.Tool,
                    Name = toolChoice.ToolName
                };
                break;
            default:
                return request;
        }

        return request with { ToolChoice = anthropicToolChoice };
    }
}
