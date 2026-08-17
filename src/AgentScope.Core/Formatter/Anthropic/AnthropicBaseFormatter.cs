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
/// Anthropic API 序列化选项
/// </summary>
public static class AnthropicSerializerOptions
{
    /// <summary>
    /// Default JSON serializer options: snake_case naming, skip null values.
    /// 默认 JSON 序列化选项：蛇形命名、忽略 null 值
    /// </summary>
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
/// 此类处理：
/// - System message extraction and application (Anthropic requires system via system parameter)
///   系统消息提取和应用（Anthropic 要求系统消息通过 system 参数传递）
/// - Tool choice configuration with GenerateOptions
///   工具选择配置
///
/// Java参考: io.agentscope.core.formatter.anthropic.AnthropicBaseFormatter
/// </summary>
public abstract class AnthropicBaseFormatter
{
    /// <summary>
    /// Default max tokens for Anthropic API (required parameter).
    /// Anthropic API 默认最大 token 数（必填参数）
    /// </summary>
    protected const int DefaultMaxTokens = 4096;

    /// <summary>
    /// Format messages to Anthropic request.
    /// 格式化消息为 Anthropic 请求
    /// </summary>
    /// <param name="messages">AgentScope messages / AgentScope 消息列表</param>
    /// <param name="options">Generation options / 生成选项</param>
    /// <returns>Anthropic request / Anthropic 请求对象</returns>
    public virtual AnthropicRequest Format(List<Msg> messages, GenerateOptions? options = null)
    {
        // 提取系统消息（Anthropic 使用独立的 system 参数）
        // Extract system message (Anthropic uses separate system parameter)
        var systemMessages = AnthropicMessageConverter.ExtractSystemMessage(messages);

        // 转换剩余消息
        // Convert remaining messages
        var filteredMessages = messages;
        if (systemMessages != null && messages.Count > 0 && messages[0].Role == "system")
        {
            // 跳过第一条系统消息，已提取到 system 参数
            // Skip first system message as it's extracted to system parameter
            filteredMessages = messages.Skip(1).ToList();
        }

        var anthropicMessages = AnthropicMessageConverter.Convert(filteredMessages);

        // 构建请求
        // Build request
        var request = new AnthropicRequest
        {
            Model = GetModelName(options),
            Messages = anthropicMessages,
            System = systemMessages,
            MaxTokens = options?.MaxTokens ?? DefaultMaxTokens
        };

        // 应用生成选项
        // Apply generation options
        request = ApplyOptions(request, options);

        return request;
    }

    /// <summary>
    /// Parse Anthropic response to ChatResponse.
    /// 解析 Anthropic 响应
    /// </summary>
    /// <param name="response">Anthropic response / Anthropic 响应</param>
    /// <param name="startTime">Request start time / 请求开始时间</param>
    /// <returns>AgentScope ChatResponse / AgentScope 聊天响应</returns>
    public virtual Model.ChatResponse Parse(AnthropicResponse response, DateTime startTime)
    {
        return AnthropicResponseParser.ParseMessage(response, startTime);
    }

    /// <summary>
    /// Parse JSON response string to ParsedResponse.
    /// 解析 JSON 响应字符串
    /// </summary>
    /// <param name="json">JSON response string / JSON 响应字符串</param>
    /// <returns>Parsed response or null / 解析后的响应或 null</returns>
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
    /// 将 AnthropicResponse 转换为 ParsedResponse
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

        // 合并所有文本内容
        // Join all text content parts
        result.TextContent = string.Join("\n", textParts);
        if (toolCalls.Count > 0)
        {
            result.ToolCalls = toolCalls;
        }

        return result;
    }

    /// <summary>
    /// Get model name from options or use default.
    /// 从选项获取模型名称或使用默认值
    /// </summary>
    protected virtual string GetModelName(GenerateOptions? options)
    {
        // 检查选项中是否指定了模型
        // Check for model in options metadata
        if (options?.AdditionalBodyParams?.TryGetValue("model", out var modelObj) == true &&
            modelObj is string modelStr)
        {
            return modelStr;
        }

        // 默认使用 Claude 3.5 Sonnet
        // Default to Claude 3.5 Sonnet
        return "claude-3-5-sonnet-20241022";
    }

    /// <summary>
    /// Apply generation options to Anthropic request.
    /// 应用生成选项到 Anthropic 请求
    /// </summary>
    protected virtual AnthropicRequest ApplyOptions(AnthropicRequest request, GenerateOptions? options)
    {
        if (options == null)
        {
            return request;
        }

        // Temperature / 温度
        if (options.Temperature.HasValue)
        {
            request = request with { Temperature = options.Temperature.Value };
        }

        // Top P / Top-p 采样
        if (options.TopP.HasValue)
        {
            request = request with { TopP = options.TopP.Value };
        }

        // Top K / Top-k 采样
        if (options.TopK.HasValue)
        {
            request = request with { TopK = options.TopK.Value };
        }

        // 最大 token 数（Format 中已设置，此处允许覆盖）
        // Max tokens (already set in Format, but allow override)
        if (options.MaxTokens.HasValue)
        {
            request = request with { MaxTokens = options.MaxTokens.Value };
        }

        // 停止序列
        // Stop sequences
        if (options.Stop?.Count > 0)
        {
            request = request with { StopSequences = options.Stop };
        }

        // 应用工具（如果指定）
        // Apply tools if specified
        if (options.AdditionalBodyParams?.TryGetValue("tools", out var toolsObj) == true &&
            toolsObj is List<ToolSchema> tools)
        {
            request = ApplyTools(request, tools, options);
        }

        // 应用工具选择（如果指定）
        // Apply tool choice if specified
        if (options.AdditionalBodyParams?.TryGetValue("tool_choice", out var toolChoiceObj) == true &&
            toolChoiceObj is ToolChoice toolChoice)
        {
            request = ApplyToolChoice(request, toolChoice);
        }

        // 应用思考配置（Claude 3.7 Sonnet 扩展思考功能）
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
    /// 应用工具模式到请求
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
    /// 应用工具选择配置到请求
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
                // Anthropic 没有 None 类型，使用 Any 作为最接近的等效项
                // Anthropic doesn't have None, use Any as closest equivalent
                anthropicToolChoice = new AnthropicToolChoice { Type = AnthropicToolChoiceType.Any };
                break;
            case ToolChoiceType.Required:
                // Anthropic 没有 Required 类型，使用 Any 强制使用工具
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
