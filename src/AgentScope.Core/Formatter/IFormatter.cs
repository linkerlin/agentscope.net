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

using System;
using System.Collections.Generic;
using AgentScope.Core.Message;
using AgentScope.Core.Model;

namespace AgentScope.Core.Formatter;

/// <summary>
/// Formatter 接口，用于在 AgentScope 和特定提供商格式之间进行转换
/// Formatter interface for converting between AgentScope and provider-specific formats
/// 
/// 职责：
/// Responsibilities:
/// 1. 将 Msg 对象转换为提供商特定的请求格式
///    Converting Msg objects to provider-specific request format
/// 2. 将提供商特定的响应转换回 AgentScope ChatResponse
///    Converting provider-specific responses back to AgentScope ChatResponse
/// 3. 应用生成选项到提供商特定的请求构建器
///    Applying generation options to provider-specific request builders
/// 4. 应用工具模式到提供商特定的请求构建器
///    Applying tool schemas to provider-specific request builders
/// </summary>
/// <typeparam name="TRequest">提供商特定的请求消息类型 Provider-specific request message type</typeparam>
/// <typeparam name="TResponse">提供商特定的响应类型 Provider-specific response type</typeparam>
/// <typeparam name="TParams">提供商特定的请求参数构建器类型 Provider-specific request parameters builder type</typeparam>
public interface IFormatter<TRequest, TResponse, TParams>
{
    /// <summary>
    /// 将 AgentScope 消息格式化为提供商特定的请求格式
    /// Format AgentScope messages to provider-specific request format
    /// </summary>
    List<TRequest> Format(List<Msg> messages);

    /// <summary>
    /// 解析提供商特定的响应为 AgentScope ChatResponse
    /// Parse provider-specific response to AgentScope ChatResponse
    /// </summary>
    ModelResponse ParseResponse(TResponse response, DateTime startTime);

    /// <summary>
    /// 应用生成选项到提供商特定的请求参数
    /// Apply generation options to provider-specific request parameters
    /// </summary>
    void ApplyOptions(TParams paramsBuilder, GenerateOptions? options, GenerateOptions? defaultOptions);

    /// <summary>
    /// 应用工具模式到提供商特定的请求参数
    /// Apply tool schemas to provider-specific request parameters
    /// </summary>
    void ApplyTools(TParams paramsBuilder, List<ToolSchema>? tools);

    /// <summary>
    /// 应用工具模式到提供商特定的请求参数（带提供商兼容性处理）
    /// Apply tool schemas with provider compatibility handling
    /// </summary>
    void ApplyTools(TParams paramsBuilder, List<ToolSchema>? tools, string? baseUrl, string? modelName)
    {
        // 默认实现：委托给简单方法
        // Default implementation: delegate to the simpler method
        ApplyTools(paramsBuilder, tools);
    }
}

/// <summary>
/// 生成选项
/// Generation options for LLM requests
/// </summary>
public class GenerateOptions
{
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public double? TopP { get; set; }
    public int? TopK { get; set; }
    public double? FrequencyPenalty { get; set; }
    public double? PresencePenalty { get; set; }
    public List<string>? Stop { get; set; }
    public int? Seed { get; set; }
    public ResponseFormat? ResponseFormat { get; set; }
}

/// <summary>
/// 工具模式
/// Tool schema for function calling
/// </summary>
public class ToolSchema
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public bool? Strict { get; set; }
}

/// <summary>
/// 响应格式配置
/// Response format configuration for structured output
/// </summary>
public class ResponseFormat
{
    /// <summary>
    /// 响应格式类型："text", "json_object", "json_schema"
    /// Response format type: "text", "json_object", or "json_schema"
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// JSON Schema 规范（仅用于 json_schema 类型）
    /// JSON Schema specification (only for json_schema type)
    /// </summary>
    public JsonSchema? JsonSchema { get; set; }

    public static ResponseFormat Text() => new() { Type = "text" };
    
    public static ResponseFormat JsonObject() => new() { Type = "json_object" };
    
    public static ResponseFormat WithJsonSchema(JsonSchema schema) => 
        new() { Type = "json_schema", JsonSchema = schema };
}

/// <summary>
/// JSON Schema 定义
/// JSON Schema definition for structured output
/// </summary>
public class JsonSchema
{
    public string Name { get; set; } = "";
    public Dictionary<string, object>? Schema { get; set; }
    public bool? Strict { get; set; }
}

/// <summary>
/// Formatter 异常
/// Formatter exception
/// </summary>
public class FormatterException : AgentScope.Core.Exception.AgentScopeException
{
    public FormatterException(string message) : base(message) { }
    public FormatterException(string message, System.Exception innerException) 
        : base(message, innerException) { }
}
