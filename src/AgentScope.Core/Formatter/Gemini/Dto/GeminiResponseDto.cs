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

namespace AgentScope.Core.Formatter.Gemini.Dto;

/// <summary>
/// Gemini API 响应
/// Gemini API response
/// </summary>
public class GeminiResponse
{
    /// <summary>
    /// 候选响应列表
    /// Candidate responses
    /// </summary>
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }

    /// <summary>
    /// 元数据
    /// Metadata
    /// </summary>
    [JsonPropertyName("modelVersion")]
    public string? ModelVersion { get; set; }
}

/// <summary>
/// Gemini 候选响应
/// Gemini candidate response
/// </summary>
public class GeminiCandidate
{
    /// <summary>
    /// 内容
    /// Content
    /// </summary>
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }

    /// <summary>
    /// 完成原因
    /// Finish reason
    /// </summary>
    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }

    /// <summary>
    /// 安全评级
    /// Safety ratings
    /// </summary>
    [JsonPropertyName("safetyRatings")]
    public List<GeminiSafetyRating>? SafetyRatings { get; set; }
}

/// <summary>
/// Gemini 安全评级
/// Gemini safety rating
/// </summary>
public class GeminiSafetyRating
{
    /// <summary>
    /// 类别
    /// Category
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    /// <summary>
    /// 概率
    /// Probability
    /// </summary>
    [JsonPropertyName("probability")]
    public string Probability { get; set; } = "";
}
