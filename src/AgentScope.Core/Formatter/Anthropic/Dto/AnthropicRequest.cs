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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgentScope.Core.Formatter.Anthropic.Dto;

/// <summary>
/// Anthropic Messages API 请求
/// Anthropic Messages API request
/// 
/// Java参考: com.anthropic.models.messages.MessageCreateParams
/// </summary>
public record AnthropicRequest
{
    /// <summary>
    /// 模型名称 / Model name (e.g., "claude-3-opus-20240229", "claude-3-5-sonnet-20241022")
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// 消息列表 / List of messages
    /// </summary>
    [JsonPropertyName("messages")]
    public required List<AnthropicMessage> Messages { get; init; }

    /// <summary>
    /// 系统提示（Anthropic 使用单独的 system 参数）
    /// System prompt (Anthropic uses separate system parameter, not as a message)
    /// </summary>
    [JsonPropertyName("system")]
    public List<AnthropicSystemMessage>? System { get; init; }

    /// <summary>
    /// 最大生成 token 数（Anthropic 要求必填）
    /// Maximum number of tokens to generate (required for Anthropic)
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public required int MaxTokens { get; init; }

    /// <summary>
    /// 温度参数 (0.0 - 1.0)
    /// Temperature parameter (0.0 - 1.0)
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }

    /// <summary>
    /// Top P 参数
    /// Top P parameter
    /// </summary>
    [JsonPropertyName("top_p")]
    public double? TopP { get; init; }

    /// <summary>
    /// Top K 参数
    /// Top K parameter
    /// </summary>
    [JsonPropertyName("top_k")]
    public int? TopK { get; init; }

    /// <summary>
    /// 停止序列
    /// Stop sequences
    /// </summary>
    [JsonPropertyName("stop_sequences")]
    public List<string>? StopSequences { get; init; }

    /// <summary>
    /// 是否流式输出
    /// Whether to stream the response
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; init; }

    /// <summary>
    /// 工具列表
    /// List of tools available to the model
    /// </summary>
    [JsonPropertyName("tools")]
    public List<AnthropicTool>? Tools { get; init; }

    /// <summary>
    /// 工具选择配置
    /// Tool choice configuration
    /// </summary>
    [JsonPropertyName("tool_choice")]
    public AnthropicToolChoice? ToolChoice { get; init; }

    /// <summary>
    /// 思考配置（Claude 3.7 Sonnet extended thinking）
    /// Thinking configuration (Claude 3.7 Sonnet extended thinking)
    /// </summary>
    [JsonPropertyName("thinking")]
    public ThinkingConfig? Thinking { get; init; }

    /// <summary>
    /// 元数据
    /// Metadata for the request
    /// </summary>
    [JsonPropertyName("metadata")]
    public AnthropicMetadata? Metadata { get; init; }
}

/// <summary>
/// 思考配置
/// Thinking configuration for Claude 3.7 Sonnet
/// </summary>
public record ThinkingConfig
{
    /// <summary>
    /// 思考类型 / Thinking type ("enabled")
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// 预算 tokens（用于思考的最大 token 数）
    /// Budget tokens (maximum tokens to use for thinking)
    /// </summary>
    [JsonPropertyName("budget_tokens")]
    public required int BudgetTokens { get; init; }
}

/// <summary>
/// 请求元数据
/// Request metadata
/// </summary>
public record AnthropicMetadata
{
    /// <summary>
    /// 用户标识
    /// User identifier
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }
}
