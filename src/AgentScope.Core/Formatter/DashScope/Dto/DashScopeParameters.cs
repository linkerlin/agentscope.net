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

namespace AgentScope.Core.Formatter.DashScope.Dto;

/// <summary>
/// DashScope API parameters DTO, containing generation configuration.
/// DashScope API 参数 DTO，包含生成配置选项。
///
/// 包括温度、采样参数、停止序列、工具等设置。
/// Includes temperature, sampling parameters, stop sequences, tools, etc.
///
/// Java参考: io.agentscope.core.formatter.dashscope.dto.DashScopeParameters
/// </summary>
public class DashScopeParameters
{
    /// <summary>
    /// 结果格式，聊天补全应设为 "message"。
    /// Result format, should be "message" for chat completions.
    /// </summary>
    [JsonPropertyName("result_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResultFormat { get; set; } = "message";

    /// <summary>
    /// 是否使用增量输出，用于流式响应。
    /// Whether to use incremental output for streaming responses.
    /// </summary>
    [JsonPropertyName("incremental_output")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncrementalOutput { get; set; }

    /// <summary>
    /// 采样温度 (0.0-2.0)，控制输出随机性。
    /// Sampling temperature (0.0-2.0), controlling output randomness.
    /// </summary>
    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Temperature { get; set; }

    /// <summary>
    /// 核采样参数 (0.0-1.0)，每次只从概率累积到 top_p 的 token 中采样。
    /// Nucleus sampling parameter (0.0-1.0), samples only from tokens whose cumulative probability reaches top_p.
    /// </summary>
    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TopP { get; set; }

    /// <summary>
    /// Top-K 采样参数，从概率最高的 K 个 token 中采样。
    /// Top-K sampling parameter, samples from the K tokens with highest probability.
    /// </summary>
    [JsonPropertyName("top_k")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TopK { get; set; }

    /// <summary>
    /// 最大生成 token 数，限制响应长度。
    /// Maximum tokens to generate, limiting response length.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// 停止序列，遇到指定序列时停止生成。
    /// Stop sequences, generation stops when encountering any specified sequence.
    /// </summary>
    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Stop { get; set; }

    /// <summary>
    /// 启用思考/推理模式，用于通义千问推理模型。
    /// Enable thinking/reasoning mode for Qwen reasoning models.
    /// </summary>
    [JsonPropertyName("enable_thinking")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? EnableThinking { get; set; }

    /// <summary>
    /// 启用搜索模式，允许模型联网检索实时信息。
    /// Enable search mode, allowing the model to retrieve real-time information from the web.
    /// </summary>
    [JsonPropertyName("enable_search")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? EnableSearch { get; set; }

    /// <summary>
    /// 思考预算，控制推理过程的 token 预算。
    /// Token budget for thinking, controlling the reasoning process budget.
    /// </summary>
    [JsonPropertyName("thinking_budget")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ThinkingBudget { get; set; }

    /// <summary>
    /// 可用工具列表，模型可调用的函数集合。
    /// List of available tools that the model can invoke.
    /// </summary>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<DashScopeTool>? Tools { get; set; }

    /// <summary>
    /// 工具选择配置，可以是 "auto", "none" 或指定工具对象。
    /// Tool choice configuration, can be "auto", "none", or a specific tool object.
    /// </summary>
    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ToolChoice { get; set; }

    /// <summary>
    /// 随机种子，用于可重现的生成。
    /// Random seed for reproducibility.
    /// </summary>
    [JsonPropertyName("seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Seed { get; set; }

    /// <summary>
    /// 频率惩罚 (-2.0 到 2.0)，降低重复 token 的概率。
    /// Frequency penalty (-2.0 to 2.0), reducing the probability of repeated tokens.
    /// </summary>
    [JsonPropertyName("frequency_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? FrequencyPenalty { get; set; }

    /// <summary>
    /// 存在惩罚 (-2.0 到 2.0)，惩罚已出现的 token，鼓励话题多样性。
    /// Presence penalty (-2.0 to 2.0), penalizing tokens that have already appeared, encouraging topic diversity.
    /// </summary>
    [JsonPropertyName("presence_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? PresencePenalty { get; set; }

    /// <summary>
    /// 重复惩罚 (0.0 到 2.0)，控制重复内容的惩罚力度。
    /// Repetition penalty (0.0 to 2.0), controlling the penalty strength for repetitive content.
    /// </summary>
    [JsonPropertyName("repetition_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? RepetitionPenalty { get; set; }

    /// <summary>
    /// 响应格式配置（如 JSON 模式），控制输出格式。
    /// The configuration for the response format (e.g., JSON mode), controlling output format.
    /// </summary>
    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResponseFormat? ResponseFormat { get; set; }
}

/// <summary>
/// DashScope 响应格式配置，指定输出格式。
/// Response format configuration for DashScope, specifying the output format.
/// </summary>
public class ResponseFormat
{
    /// <summary>
    /// 响应格式类型："text" 或 "json_object"
    /// Response format type: "text" or "json_object"
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }
}
