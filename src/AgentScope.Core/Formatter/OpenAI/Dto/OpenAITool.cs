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
/// OpenAI 工具定义
/// OpenAI tool definition
/// 
/// 对应 Java: io.agentscope.core.formatter.openai.dto.OpenAITool
/// </summary>
public record OpenAITool
{
    /// <summary>
    /// 工具类型，目前只支持 "function"
    /// Tool type, currently only "function" is supported
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    /// <summary>
    /// 函数定义
    /// Function definition
    /// </summary>
    [JsonPropertyName("function")]
    public required OpenAIToolFunction Function { get; init; }

    /// <summary>
    /// 是否启用严格模式（JSON Schema严格验证）
    /// Whether to enable strict mode (JSON Schema strict validation)
    /// </summary>
    [JsonPropertyName("strict")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Strict { get; init; }
}

/// <summary>
/// OpenAI 函数定义
/// OpenAI function definition
/// </summary>
public record OpenAIToolFunction
{
    /// <summary>
    /// 函数名称
    /// Function name
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// 函数描述
    /// Function description
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    /// <summary>
    /// 函数参数（JSON Schema格式）
    /// Function parameters (JSON Schema format)
    /// </summary>
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Parameters { get; init; }

    /// <summary>
    /// 是否使用严格模式
    /// Whether to use strict mode
    /// </summary>
    [JsonPropertyName("strict")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Strict { get; init; }
}

/// <summary>
/// OpenAI 工具调用
/// OpenAI tool call
/// </summary>
public record OpenAIToolCall
{
    /// <summary>
    /// 流式增量中的工具调用序号（用于跨块合并同一工具调用）
    /// Tool call index for merging incremental deltas across stream chunks
    /// </summary>
    [JsonPropertyName("index")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Index { get; init; }

    /// <summary>
    /// 工具调用ID
    /// Tool call ID
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    /// <summary>
    /// 工具类型
    /// Tool type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    /// <summary>
    /// 函数调用信息
    /// Function call information
    /// </summary>
    [JsonPropertyName("function")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIFunctionCall? Function { get; init; }
}

/// <summary>
/// OpenAI 函数调用
/// OpenAI function call
/// </summary>
public record OpenAIFunctionCall
{
    /// <summary>
    /// 函数名称
    /// Function name
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>
    /// 函数参数（JSON字符串）
    /// Function arguments (JSON string)
    /// </summary>
    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Arguments { get; init; }
}

/// <summary>
/// 工具选择配置
/// Tool choice configuration
/// </summary>
public record OpenAIToolChoice
{
    /// <summary>
    /// 选择类型：auto, none, required, 或指定函数
    /// Choice type: auto, none, required, or specific function
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; init; }

    /// <summary>
    /// 指定的函数（当type为function时）
    /// Specific function (when type is function)
    /// </summary>
    [JsonPropertyName("function")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIToolChoiceFunction? Function { get; init; }
}

/// <summary>
/// 工具选择函数
/// Tool choice function
/// </summary>
public record OpenAIToolChoiceFunction
{
    /// <summary>
    /// 函数名称
    /// Function name
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}
