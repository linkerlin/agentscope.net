using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AgentScope.Core.Formatter.OpenAI.Dto;

namespace AgentScope.Core.Formatter.OpenAI;

/// <summary>
/// OpenAI 响应解析器
/// OpenAI response parser
/// 
/// 解析OpenAI API响应并提取相关信息
/// Parses OpenAI API responses and extracts relevant information
/// 
/// Java参考: io.agentscope.core.formatter.openai.OpenAIResponseParser
/// </summary>
public static class OpenAIResponseParser
{
    /// <summary>
    /// 解析完整响应
    /// Parse complete response
    /// </summary>
    /// <param name="response">OpenAI API响应 / OpenAI API response</param>
    /// <returns>解析后的响应信息 / Parsed response information</returns>
    public static ParsedResponse ParseResponse(OpenAIResponse response)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        var parsed = new ParsedResponse
        {
            Id = response.Id,
            Model = response.Model,
            Created = response.Created,
            SystemFingerprint = response.SystemFingerprint
        };

        // 解析选择列表
        // Parse choices
        if (response.Choices != null && response.Choices.Count > 0)
        {
            var firstChoice = response.Choices[0];
            
            // 提取文本内容
            // Extract text content
            parsed.TextContent = ExtractTextContent(firstChoice);
            
            // 提取工具调用
            // Extract tool calls
            parsed.ToolCalls = ExtractToolCalls(firstChoice);
            
            // 提取推理内容（o1系列）
            // Extract reasoning content (o1 series)
            parsed.ReasoningContent = ExtractReasoningContent(firstChoice);
            
            // 提取完成原因
            // Extract finish reason
            parsed.FinishReason = firstChoice.FinishReason;
            
            // 提取索引
            // Extract index
            parsed.Index = firstChoice.Index;
        }

        // 解析Token使用情况
        // Parse token usage
        if (response.Usage != null)
        {
            parsed.Usage = new TokenUsage
            {
                PromptTokens = response.Usage.PromptTokens,
                CompletionTokens = response.Usage.CompletionTokens,
                TotalTokens = response.Usage.TotalTokens
            };

            // 解析详细token信息
            // Parse detailed token information
            if (response.Usage.CompletionTokensDetails != null)
            {
                parsed.Usage.ReasoningTokens = response.Usage.CompletionTokensDetails.ReasoningTokens;
            }
        }

        return parsed;
    }

    /// <summary>
    /// 提取文本内容
    /// Extract text content
    /// </summary>
    private static string? ExtractTextContent(OpenAIChoice choice)
    {
        if (choice.Message == null)
        {
            return null;
        }

        var content = choice.Message.Content;
        
        if (content == null)
        {
            return null;
        }

        // 如果content是字符串
        // If content is string
        if (content is string text)
        {
            return text;
        }

        // 如果content是JsonElement (从System.Text.Json反序列化)
        // If content is JsonElement (deserialized from System.Text.Json)
        if (content is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return jsonElement.GetString();
            }
            
            // 如果是数组，提取所有text部分
            // If array, extract all text parts
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var textBuilder = new StringBuilder();
                foreach (var element in jsonElement.EnumerateArray())
                {
                    if (element.TryGetProperty("type", out var typeProp) &&
                        typeProp.GetString() == "text" &&
                        element.TryGetProperty("text", out var textProp))
                    {
                        var partText = textProp.GetString();
                        if (!string.IsNullOrWhiteSpace(partText))
                        {
                            if (textBuilder.Length > 0)
                            {
                                textBuilder.AppendLine();
                            }
                            textBuilder.Append(partText);
                        }
                    }
                }
                return textBuilder.ToString();
            }
        }

        // 如果content是数组，提取所有text部分
        // If content is array, extract all text parts
        if (content is List<OpenAIMessageContent> contentParts)
        {
            var textBuilder = new StringBuilder();
            foreach (var part in contentParts)
            {
                if (part.Type == "text" && !string.IsNullOrWhiteSpace(part.Text))
                {
                    if (textBuilder.Length > 0)
                    {
                        textBuilder.AppendLine();
                    }
                    textBuilder.Append(part.Text);
                }
            }
            return textBuilder.ToString();
        }

        return null;
    }

    /// <summary>
    /// 提取工具调用
    /// Extract tool calls
    /// </summary>
    private static List<ToolCallInfo>? ExtractToolCalls(OpenAIChoice choice)
    {
        if (choice.Message?.ToolCalls == null || choice.Message.ToolCalls.Count == 0)
        {
            return null;
        }

        var toolCalls = new List<ToolCallInfo>();

        foreach (var toolCall in choice.Message.ToolCalls)
        {
            if (toolCall.Function != null)
            {
                toolCalls.Add(new ToolCallInfo
                {
                    Id = toolCall.Id,
                    Type = toolCall.Type,
                    FunctionName = toolCall.Function.Name,
                    FunctionArguments = toolCall.Function.Arguments
                });
            }
        }

        return toolCalls.Count > 0 ? toolCalls : null;
    }

    /// <summary>
    /// 提取推理内容（o1系列模型）
    /// Extract reasoning content (o1 series models)
    /// </summary>
    private static string? ExtractReasoningContent(OpenAIChoice choice)
    {
        if (choice.Message == null)
        {
            return null;
        }

        // 如果content是数组，查找reasoning类型的内容
        // If content is array, find reasoning type content
        if (choice.Message.Content is List<OpenAIMessageContent> contentParts)
        {
            var reasoningParts = contentParts
                .Where(p => p.Type == "reasoning" && !string.IsNullOrWhiteSpace(p.Text))
                .Select(p => p.Text)
                .ToList();

            if (reasoningParts.Count > 0)
            {
                return string.Join("\n\n", reasoningParts);
            }
        }

        return null;
    }

    /// <summary>
    /// 解析流式响应块
    /// Parse streaming response chunk
    /// </summary>
    /// <param name="chunk">响应块 / Response chunk</param>
    /// <returns>解析后的块信息 / Parsed chunk information</returns>
    public static StreamChunkInfo? ParseStreamChunk(string chunk)
    {
        if (string.IsNullOrWhiteSpace(chunk))
        {
            return null;
        }

        // 移除 "data: " 前缀
        // Remove "data: " prefix
        if (chunk.StartsWith("data: "))
        {
            chunk = chunk.Substring(6);
        }

        // 检查是否是结束标记
        // Check if it's the end marker
        if (chunk.Trim() == "[DONE]")
        {
            return new StreamChunkInfo { IsDone = true };
        }

        try
        {
            var response = System.Text.Json.JsonSerializer.Deserialize<OpenAIResponse>(chunk);
            if (response == null || response.Choices == null || response.Choices.Count == 0)
            {
                return null;
            }

            var choice = response.Choices[0];
            var delta = choice.Delta;

            if (delta == null)
            {
                return null;
            }

            var chunkInfo = new StreamChunkInfo
            {
                IsDone = false,
                FinishReason = choice.FinishReason
            };

            // 提取增量内容
            // Extract delta content
            if (delta.Content is string deltaText && !string.IsNullOrEmpty(deltaText))
            {
                chunkInfo.Content = deltaText;
            }

            // 提取增量工具调用
            // Extract delta tool calls
            if (delta.ToolCalls != null && delta.ToolCalls.Count > 0)
            {
                var toolCallDeltas = new List<ToolCallDelta>();
                for (int i = 0; i < delta.ToolCalls.Count; i++)
                {
                    var tc = delta.ToolCalls[i];
                    toolCallDeltas.Add(new ToolCallDelta
                    {
                        Index = i, // 使用列表索引 / Use list index
                        Id = tc.Id,
                        Type = tc.Type,
                        FunctionName = tc.Function?.Name,
                        FunctionArguments = tc.Function?.Arguments
                    });
                }
                chunkInfo.ToolCallDeltas = toolCallDeltas;
            }

            return chunkInfo;
        }
        catch
        {
            // 解析失败，返回null
            // Parsing failed, return null
            return null;
        }
    }
}

/// <summary>
/// 解析后的响应信息
/// Parsed response information
/// </summary>
public class ParsedResponse
{
    /// <summary>
    /// 响应ID
    /// Response ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 模型名称
    /// Model name
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// 创建时间（Unix时间戳）
    /// Creation time (Unix timestamp)
    /// </summary>
    public long Created { get; set; }

    /// <summary>
    /// 系统指纹
    /// System fingerprint
    /// </summary>
    public string? SystemFingerprint { get; set; }

    /// <summary>
    /// 文本内容
    /// Text content
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// 工具调用列表
    /// Tool calls list
    /// </summary>
    public List<ToolCallInfo>? ToolCalls { get; set; }

    /// <summary>
    /// 推理内容（o1系列）
    /// Reasoning content (o1 series)
    /// </summary>
    public string? ReasoningContent { get; set; }

    /// <summary>
    /// 完成原因
    /// Finish reason
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// 选择索引
    /// Choice index
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Token使用情况
    /// Token usage
    /// </summary>
    public TokenUsage? Usage { get; set; }
}

/// <summary>
/// 工具调用信息
/// Tool call information
/// </summary>
public class ToolCallInfo
{
    /// <summary>
    /// 工具调用ID
    /// Tool call ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 工具类型
    /// Tool type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 函数名称
    /// Function name
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// 函数参数（JSON字符串）
    /// Function arguments (JSON string)
    /// </summary>
    public string? FunctionArguments { get; set; }
}

/// <summary>
/// Token使用情况
/// Token usage
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// 提示Token数量
    /// Prompt tokens
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// 完成Token数量
    /// Completion tokens
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// 总Token数量
    /// Total tokens
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// 推理Token数量（o1系列）
    /// Reasoning tokens (o1 series)
    /// </summary>
    public int? ReasoningTokens { get; set; }
}

/// <summary>
/// 流式响应块信息
/// Stream chunk information
/// </summary>
public class StreamChunkInfo
{
    /// <summary>
    /// 是否完成
    /// Is done
    /// </summary>
    public bool IsDone { get; set; }

    /// <summary>
    /// 内容
    /// Content
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 完成原因
    /// Finish reason
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// 工具调用增量
    /// Tool call deltas
    /// </summary>
    public List<ToolCallDelta>? ToolCallDeltas { get; set; }
}

/// <summary>
/// 工具调用增量
/// Tool call delta
/// </summary>
public class ToolCallDelta
{
    /// <summary>
    /// 索引
    /// Index
    /// </summary>
    public int? Index { get; set; }

    /// <summary>
    /// 工具调用ID
    /// Tool call ID
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// 工具类型
    /// Tool type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 函数名称
    /// Function name
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// 函数参数
    /// Function arguments
    /// </summary>
    public string? FunctionArguments { get; set; }
}
