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

namespace AgentScope.Core.Formatter.Anthropic;

/// <summary>
/// 生成选项
/// Generation options for Anthropic API
/// </summary>
public class GenerateOptions
{
    /// <summary>
    /// 温度参数 (0-1)
    /// Temperature (0-1)
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Top-p 采样参数 (0-1)
    /// Top-p sampling (0-1)
    /// </summary>
    public double? TopP { get; set; }

    /// <summary>
    /// Top-k 采样参数
    /// Top-k sampling
    /// </summary>
    public int? TopK { get; set; }

    /// <summary>
    /// 最大token数
    /// Maximum tokens
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// 频率惩罚
    /// Frequency penalty
    /// </summary>
    public double? FrequencyPenalty { get; set; }

    /// <summary>
    /// 存在惩罚
    /// Presence penalty
    /// </summary>
    public double? PresencePenalty { get; set; }

    /// <summary>
    /// 随机种子
    /// Random seed
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// 停止序列
    /// Stop sequences
    /// </summary>
    public List<string>? Stop { get; set; }

    /// <summary>
    /// 思考预算（Claude 3.7 Sonnet extended thinking）
    /// Thinking budget tokens
    /// </summary>
    public int? ThinkingBudget { get; set; }

    /// <summary>
    /// 响应格式
    /// Response format
    /// </summary>
    public ResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// 是否流式输出
    /// Whether to stream the response
    /// </summary>
    public bool Stream { get; set; }

    /// <summary>
    /// 额外的请求体参数
    /// Additional body parameters
    /// </summary>
    public Dictionary<string, object>? AdditionalBodyParams { get; set; }

    /// <summary>
    /// 额外的请求头
    /// Additional headers
    /// </summary>
    public Dictionary<string, string>? AdditionalHeaders { get; set; }
}

/// <summary>
/// 响应格式
/// Response format
/// </summary>
public class ResponseFormat
{
    /// <summary>
    /// 响应格式类型："text", "json_object"
    /// Response format type: "text" or "json_object"
    /// </summary>
    public string Type { get; set; } = "text";

    public static ResponseFormat Text() => new() { Type = "text" };
    
    public static ResponseFormat JsonObject() => new() { Type = "json_object" };
}
