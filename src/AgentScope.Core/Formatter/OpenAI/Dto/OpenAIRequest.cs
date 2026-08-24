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

// Copyright (c) 2024 AgentScope team.
// Licensed under the Apache License, Version 2.0.

using System.Text.Json.Serialization;

namespace AgentScope.Core.Formatter.OpenAI.Dto;

/// <summary>
/// OpenAI Chat Completions API 请求对象<br />
/// OpenAI Chat Completions API request object<br />
/// 对应 Java: io.agentscope.core.formatter.openai.dto.OpenAIRequest
/// </summary>
public record OpenAIRequest
{
    /// <summary>
    /// 模型名称，例如 "gpt-4", "gpt-3.5-turbo"<br />
    /// Model name, e.g., "gpt-4", "gpt-3.5-turbo"
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    /// <summary>
    /// 对话消息列表<br />
    /// List of conversation messages
    /// </summary>
    [JsonPropertyName("messages")]
    public required List<OpenAIMessage> Messages { get; set; }

    /// <summary>
    /// 温度参数，控制输出的随机性 (0.0-2.0)<br />
    /// Temperature parameter controlling output randomness (0.0-2.0)
    /// </summary>
    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    /// <summary>
    /// Top-p 核采样参数 (0.0-1.0)<br />
    /// Top-p nucleus sampling parameter (0.0-1.0)
    /// </summary>
    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TopP { get; set; }

    /// <summary>
    /// 生成的最大 token 数<br />
    /// Maximum number of tokens to generate
    /// </summary>
    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// 最大完成 token 数，优先于 max_tokens（用于推理模型）<br />
    /// Maximum completion tokens, takes precedence over max_tokens (for reasoning models)
    /// </summary>
    [JsonPropertyName("max_completion_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxCompletionTokens { get; set; }

    /// <summary>
    /// 频率惩罚 (-2.0 到 2.0)，降低重复词出现概率<br />
    /// Frequency penalty (-2.0 to 2.0), reduces likelihood of repetition
    /// </summary>
    [JsonPropertyName("frequency_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? FrequencyPenalty { get; set; }

    /// <summary>
    /// 存在惩罚 (-2.0 到 2.0)，鼓励谈论新话题<br />
    /// Presence penalty (-2.0 to 2.0), encourages talking about new topics
    /// </summary>
    [JsonPropertyName("presence_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PresencePenalty { get; set; }

    /// <summary>
    /// 停止序列，可为 string 或 string[]，遇到这些序列时停止生成<br />
    /// Stop sequences, can be string or string[], generation stops when these are encountered
    /// </summary>
    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Stop { get; set; }

    /// <summary>
    /// 是否启用流式返回<br />
    /// Whether to enable streaming response
    /// </summary>
    [JsonPropertyName("stream")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Stream { get; set; }

    /// <summary>
    /// 随机种子，用于可重现的结果<br />
    /// Random seed for reproducible results
    /// </summary>
    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Seed { get; set; }

    /// <summary>
    /// 可调用的工具列表<br />
    /// List of callable tools
    /// </summary>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OpenAITool>? Tools { get; set; }

    /// <summary>
    /// 工具选择策略，可为 string (auto/none/required) 或 OpenAIToolChoice<br />
    /// Tool choice strategy, can be string (auto/none/required) or OpenAIToolChoice
    /// </summary>
    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ToolChoice { get; set; }

    /// <summary>
    /// 响应格式配置，如 JSON 模式<br />
    /// Response format configuration, e.g. JSON mode
    /// </summary>
    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ResponseFormat { get; set; }

    /// <summary>
    /// 推理力度，用于 o1/o3 系列推理模型（low/medium/high）<br />
    /// Reasoning effort for o1/o3 series reasoning models (low/medium/high)
    /// </summary>
    [JsonPropertyName("reasoning_effort")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReasoningEffort { get; set; }

    /// <summary>
    /// 是否在响应中包含推理/思考内容<br />
    /// Whether to include reasoning/thinking content in the response
    /// </summary>
    [JsonPropertyName("include_reasoning")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncludeReasoning { get; set; }

    /// <summary>
    /// 用户标识，用于监控和限流<br />
    /// User identifier for monitoring and rate limiting
    /// </summary>
    [JsonPropertyName("user")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? User { get; set; }

    /// <summary>
    /// 每个请求返回的选择数量<br />
    /// Number of choices to return per request
    /// </summary>
    [JsonPropertyName("n")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? N { get; set; }

    /// <summary>
    /// Logit 偏置，调整特定 token 的出现概率<br />
    /// Logit bias modifying the likelihood of specified tokens
    /// </summary>
    [JsonPropertyName("logit_bias")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, double>? LogitBias { get; set; }

    /// <summary>
    /// 是否返回 token 级别的对数概率信息<br />
    /// Whether to return token-level log probability information
    /// </summary>
    [JsonPropertyName("logprobs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Logprobs { get; set; }

    /// <summary>
    /// 返回概率最高的 top logprobs 数量<br />
    /// Number of top logprobs to return
    /// </summary>
    [JsonPropertyName("top_logprobs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TopLogprobs { get; set; }
}
