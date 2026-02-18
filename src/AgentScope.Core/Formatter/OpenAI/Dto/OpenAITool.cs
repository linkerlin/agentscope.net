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
    /// 工具调用ID
    /// Tool call ID
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

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
    public required OpenAIFunctionCall Function { get; init; }
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
    public required string Name { get; init; }

    /// <summary>
    /// 函数参数（JSON字符串）
    /// Function arguments (JSON string)
    /// </summary>
    [JsonPropertyName("arguments")]
    public required string Arguments { get; init; }
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
