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

namespace AgentScope.Core.Formatter.DashScope;

/// <summary>
/// 生成选项
/// Generation options for DashScope API
/// </summary>
public class GenerateOptions
{
    /// <summary>
    /// 温度参数 (0-2)
    /// Temperature (0-2)
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
    /// 随机种子
    /// Random seed
    /// </summary>
    public int? Seed { get; set; }

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
    /// 停止序列
    /// Stop sequences
    /// </summary>
    public List<string>? Stop { get; set; }

    /// <summary>
    /// 是否启用思考模式（深度推理）
    /// Enable thinking mode (deep reasoning)
    /// </summary>
    public bool? EnableThinking { get; set; }

    /// <summary>
    /// 思考预算（最大思考token数）
    /// Thinking budget (max thinking tokens)
    /// </summary>
    public int? ThinkingBudget { get; set; }

    /// <summary>
    /// 是否启用增量输出
    /// Enable incremental output
    /// </summary>
    public bool? IncrementalOutput { get; set; }

    /// <summary>
    /// 是否启用搜索
    /// Enable search
    /// </summary>
    public bool? EnableSearch { get; set; }

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
    /// 工具列表
    /// List of tools
    /// </summary>
    public List<ToolInfo>? Tools { get; set; }

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
/// 工具信息
/// Tool information
/// </summary>
public class ToolInfo
{
    /// <summary>
    /// 工具名称
    /// Tool name
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 工具描述
    /// Tool description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 工具参数
    /// Tool parameters
    /// </summary>
    public ToolParameters? Parameters { get; set; }
}

/// <summary>
/// 工具参数
/// Tool parameters
/// </summary>
public class ToolParameters
{
    /// <summary>
    /// 参数类型
    /// Parameter type
    /// </summary>
    public string Type { get; set; } = "object";

    /// <summary>
    /// 属性定义
    /// Property definitions
    /// </summary>
    public Dictionary<string, ToolProperty>? Properties { get; set; }

    /// <summary>
    /// 必需参数列表
    /// Required parameters
    /// </summary>
    public List<string>? Required { get; set; }
}

/// <summary>
/// 工具属性
/// Tool property
/// </summary>
public class ToolProperty
{
    /// <summary>
    /// 属性类型
    /// Property type
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// 属性描述
    /// Property description
    /// </summary>
    public string? Description { get; set; }
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
