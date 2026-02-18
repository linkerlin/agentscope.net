// Copyright (c) 2024 AgentScope team.
// Licensed under the Apache License, Version 2.0.

using System.Text.Json.Serialization;

namespace AgentScope.Core.Formatter.OpenAI.Dto;

/// <summary>
/// OpenAI Chat Completions API 请求对象
/// OpenAI Chat Completions API request object
/// 
/// 对应 Java: io.agentscope.core.formatter.openai.dto.OpenAIRequest
/// </summary>
public record OpenAIRequest
{
    /// <summary>
    /// 模型名称，例如 "gpt-4", "gpt-3.5-turbo"
    /// Model name, e.g., "gpt-4", "gpt-3.5-turbo"
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    /// <summary>
    /// 消息列表
    /// List of messages
    /// </summary>
    [JsonPropertyName("messages")]
    public required List<OpenAIMessage> Messages { get; set; }

    /// <summary>
    /// 温度参数 (0.0-2.0)
    /// Temperature parameter (0.0-2.0)
    /// </summary>
    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    /// <summary>
    /// Top-p 采样参数 (0.0-1.0)
    /// Top-p sampling parameter (0.0-1.0)
    /// </summary>
    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TopP { get; set; }

    /// <summary>
    /// 生成的最大token数
    /// Maximum number of tokens to generate
    /// </summary>
    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// 生成的最大完成token数（优先于max_tokens）
    /// Maximum number of completion tokens (takes precedence over max_tokens)
    /// </summary>
    [JsonPropertyName("max_completion_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxCompletionTokens { get; set; }

    /// <summary>
    /// 频率惩罚 (-2.0 to 2.0)
    /// Frequency penalty (-2.0 to 2.0)
    /// </summary>
    [JsonPropertyName("frequency_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? FrequencyPenalty { get; set; }

    /// <summary>
    /// 存在惩罚 (-2.0 to 2.0)
    /// Presence penalty (-2.0 to 2.0)
    /// </summary>
    [JsonPropertyName("presence_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PresencePenalty { get; set; }

    /// <summary>
    /// 停止序列
    /// Stop sequences
    /// </summary>
    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Stop { get; set; } // Can be string or string[]

    /// <summary>
    /// 是否流式返回
    /// Whether to stream the response
    /// </summary>
    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Stream { get; set; }

    /// <summary>
    /// 随机种子
    /// Random seed
    /// </summary>
    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Seed { get; set; }

    /// <summary>
    /// 工具列表
    /// List of tools
    /// </summary>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OpenAITool>? Tools { get; set; }

    /// <summary>
    /// 工具选择策略
    /// Tool choice strategy
    /// </summary>
    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ToolChoice { get; set; } // Can be string or OpenAIToolChoice

    /// <summary>
    /// 响应格式配置
    /// Response format configuration
    /// </summary>
    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ResponseFormat { get; set; }

    /// <summary>
    /// 推理力度（o1系列模型）
    /// Reasoning effort (for o1 series models)
    /// </summary>
    [JsonPropertyName("reasoning_effort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningEffort { get; set; }

    /// <summary>
    /// 是否包含推理内容
    /// Whether to include reasoning content
    /// </summary>
    [JsonPropertyName("include_reasoning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncludeReasoning { get; set; }

    /// <summary>
    /// 用户标识
    /// User identifier
    /// </summary>
    [JsonPropertyName("user")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? User { get; set; }

    /// <summary>
    /// 返回的选择数量
    /// Number of choices to return
    /// </summary>
    [JsonPropertyName("n")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? N { get; set; }

    /// <summary>
    /// Logit偏置
    /// Logit bias
    /// </summary>
    [JsonPropertyName("logit_bias")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, double>? LogitBias { get; set; }

    /// <summary>
    /// 是否返回logprobs
    /// Whether to return logprobs
    /// </summary>
    [JsonPropertyName("logprobs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Logprobs { get; set; }

    /// <summary>
    /// Top logprobs数量
    /// Number of top logprobs
    /// </summary>
    [JsonPropertyName("top_logprobs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TopLogprobs { get; set; }
}
