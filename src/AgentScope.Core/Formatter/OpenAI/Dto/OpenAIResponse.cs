// Copyright (c) 2024 AgentScope team.
// Licensed under the Apache License, Version 2.0.

using System.Text.Json.Serialization;

namespace AgentScope.Core.Formatter.OpenAI.Dto;

/// <summary>
/// OpenAI Chat Completions API 响应对象
/// OpenAI Chat Completions API response object
/// 
/// 对应 Java: io.agentscope.core.formatter.openai.dto.OpenAIResponse
/// </summary>
public record OpenAIResponse
{
    /// <summary>
    /// 响应ID
    /// Response ID
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// 对象类型
    /// Object type
    /// </summary>
    [JsonPropertyName("object")]
    public required string Object { get; init; }

    /// <summary>
    /// 创建时间戳
    /// Creation timestamp
    /// </summary>
    [JsonPropertyName("created")]
    public required long Created { get; init; }

    /// <summary>
    /// 模型名称
    /// Model name
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// 选择列表
    /// List of choices
    /// </summary>
    [JsonPropertyName("choices")]
    public required List<OpenAIChoice> Choices { get; init; }

    /// <summary>
    /// Token使用统计
    /// Token usage statistics
    /// </summary>
    [JsonPropertyName("usage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIUsage? Usage { get; init; }

    /// <summary>
    /// 系统指纹
    /// System fingerprint
    /// </summary>
    [JsonPropertyName("system_fingerprint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SystemFingerprint { get; init; }

    /// <summary>
    /// 错误信息（如果有）
    /// Error information (if any)
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIError? Error { get; init; }
}

/// <summary>
/// OpenAI 选择对象
/// OpenAI choice object
/// </summary>
public record OpenAIChoice
{
    /// <summary>
    /// 选择索引
    /// Choice index
    /// </summary>
    [JsonPropertyName("index")]
    public required int Index { get; init; }

    /// <summary>
    /// 消息内容
    /// Message content
    /// </summary>
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIMessage? Message { get; init; }

    /// <summary>
    /// Delta内容（流式响应）
    /// Delta content (for streaming)
    /// </summary>
    [JsonPropertyName("delta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIMessage? Delta { get; init; }

    /// <summary>
    /// 结束原因：stop, length, tool_calls, content_filter
    /// Finish reason: stop, length, tool_calls, content_filter
    /// </summary>
    [JsonPropertyName("finish_reason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FinishReason { get; init; }

    /// <summary>
    /// Logprobs信息
    /// Logprobs information
    /// </summary>
    [JsonPropertyName("logprobs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Logprobs { get; init; }
}

/// <summary>
/// Token使用统计
/// Token usage statistics
/// </summary>
public record OpenAIUsage
{
    /// <summary>
    /// 提示token数
    /// Prompt tokens
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public required int PromptTokens { get; init; }

    /// <summary>
    /// 完成token数
    /// Completion tokens
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public required int CompletionTokens { get; init; }

    /// <summary>
    /// 总token数
    /// Total tokens
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public required int TotalTokens { get; init; }

    /// <summary>
    /// 完成token详情
    /// Completion tokens details
    /// </summary>
    [JsonPropertyName("completion_tokens_details")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAICompletionTokensDetails? CompletionTokensDetails { get; init; }
}

/// <summary>
/// 完成token详情
/// Completion tokens details
/// </summary>
public record OpenAICompletionTokensDetails
{
    /// <summary>
    /// 推理token数
    /// Reasoning tokens
    /// </summary>
    [JsonPropertyName("reasoning_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ReasoningTokens { get; init; }

    /// <summary>
    /// 音频token数
    /// Audio tokens
    /// </summary>
    [JsonPropertyName("audio_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AudioTokens { get; init; }
}

/// <summary>
/// OpenAI 错误对象
/// OpenAI error object
/// </summary>
public record OpenAIError
{
    /// <summary>
    /// 错误消息
    /// Error message
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// 错误类型
    /// Error type
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    /// <summary>
    /// 错误参数
    /// Error parameter
    /// </summary>
    [JsonPropertyName("param")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Param { get; init; }

    /// <summary>
    /// 错误代码
    /// Error code
    /// </summary>
    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; init; }
}
