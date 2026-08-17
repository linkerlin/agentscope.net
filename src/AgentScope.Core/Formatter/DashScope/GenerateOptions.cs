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
[Obsolete("使用 AgentScope.Core.Formatter.GenerateOptions 替代")]
public class GenerateOptions
{
    /// <summary>
    /// 温度参数 (0-2)，控制输出的随机性。值越高，输出越随机。
    /// Temperature (0-2), controls randomness of output. Higher values produce more random outputs.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Top-p 采样参数 (0-1)，核采样。模型只考虑累积概率达到 top_p 的 token。
    /// Top-p sampling (0-1), nucleus sampling. Model only considers tokens with cumulative probability up to top_p.
    /// </summary>
    public double? TopP { get; set; }

    /// <summary>
    /// Top-k 采样参数，从概率最高的 k 个 token 中采样。
    /// Top-k sampling, samples from the k tokens with the highest probability.
    /// </summary>
    public int? TopK { get; set; }

    /// <summary>
    /// 最大 token 数，限制生成的最大长度。
    /// Maximum tokens, limits the maximum length of generated content.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// 随机种子，用于可重现的生成结果。
    /// Random seed for reproducible generation results.
    /// </summary>
    public int? Seed { get; set; }

    /// <summary>
    /// 频率惩罚，减少重复的 token。值越大，模型越倾向于避免重复。
    /// Frequency penalty, reduces repetitive tokens. Higher values make the model avoid repetition more.
    /// </summary>
    public double? FrequencyPenalty { get; set; }

    /// <summary>
    /// 存在惩罚，惩罚已出现的 token，增加话题多样性。
    /// Presence penalty, penalizes tokens that have already appeared, increasing topic diversity.
    /// </summary>
    public double? PresencePenalty { get; set; }

    /// <summary>
    /// 停止序列，当模型输出遇到这些序列时停止生成。
    /// Stop sequences, generation stops when the model outputs any of these sequences.
    /// </summary>
    public List<string>? Stop { get; set; }

    /// <summary>
    /// 是否启用思考模式（深度推理），用于通义千问推理模型。
    /// Enable thinking mode (deep reasoning) for Qwen reasoning models.
    /// </summary>
    public bool? EnableThinking { get; set; }

    /// <summary>
    /// 思考预算，最大思考 token 数，控制推理深度。
    /// Thinking budget, max thinking tokens, controls reasoning depth.
    /// </summary>
    public int? ThinkingBudget { get; set; }

    /// <summary>
    /// 是否启用增量输出，用于流式响应中的增量更新。
    /// Enable incremental output for delta updates in streaming responses.
    /// </summary>
    public bool? IncrementalOutput { get; set; }

    /// <summary>
    /// 是否启用搜索能力，允许模型联网搜索实时信息。
    /// Enable search capability, allowing the model to search the web for real-time information.
    /// </summary>
    public bool? EnableSearch { get; set; }

    /// <summary>
    /// 响应格式，指定输出格式（如文本或 JSON）。
    /// Response format, specifying the output format (e.g., text or JSON).
    /// </summary>
    public ResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// 是否流式输出，启用 Server-Sent Events (SSE) 流式响应。
    /// Whether to stream the response via Server-Sent Events (SSE).
    /// </summary>
    public bool Stream { get; set; }

    /// <summary>
    /// 工具列表，模型可以调用的函数工具集合。
    /// List of tools that the model can call.
    /// </summary>
    public List<ToolInfo>? Tools { get; set; }

    /// <summary>
    /// 额外的请求体参数，用于传递 API 特定的扩展字段。
    /// Additional body parameters for API-specific extension fields.
    /// </summary>
    public Dictionary<string, object>? AdditionalBodyParams { get; set; }

    /// <summary>
    /// 额外的请求头，用于传递 API 特定的自定义头部。
    /// Additional headers for API-specific custom headers.
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
    /// 工具描述，说明工具的用途。
    /// Tool description, explaining the purpose of the tool.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 工具参数定义，描述函数调用的输入参数。
    /// Tool parameters defining the input arguments for the function call.
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
    /// 参数类型，通常为 "object"。
    /// Parameter type, typically "object".
    /// </summary>
    public string Type { get; set; } = "object";

    /// <summary>
    /// 属性定义，每个属性的名称到定义的映射。
    /// Property definitions, mapping property names to their definitions.
    /// </summary>
    public Dictionary<string, ToolProperty>? Properties { get; set; }

    /// <summary>
    /// 必需参数列表，列出调用该工具时必须提供的属性名。
    /// Required parameters, listing property names that must be provided when calling this tool.
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
    /// 属性类型，如 "string", "number", "integer" 等。
    /// Property type, e.g., "string", "number", "integer", etc.
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// 属性描述，说明该属性的含义。
    /// Property description, explaining the meaning of this property.
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
    /// 响应格式类型："text" 或 "json_object"
    /// Response format type: "text" or "json_object"
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// 创建文本格式的 ResponseFormat 实例。
    /// Create a text format ResponseFormat instance.
    /// </summary>
    public static ResponseFormat Text() => new() { Type = "text" };

    /// <summary>
    /// 创建 JSON 对象格式的 ResponseFormat 实例。
    /// Create a JSON object format ResponseFormat instance.
    /// </summary>
    public static ResponseFormat JsonObject() => new() { Type = "json_object" };
}
