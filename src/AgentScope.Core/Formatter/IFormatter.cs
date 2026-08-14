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
/// 工具选择类型
/// Tool choice type
/// </summary>
public enum ToolChoiceType
{
    Auto,
    None,
    Required,
    Specific
}

/// <summary>
/// 工具选择配置
/// Tool choice configuration
/// </summary>
public class ToolChoice
{
    public ToolChoiceType Type { get; set; }
    public string? ToolName { get; set; }

    public static ToolChoice Auto() => new() { Type = ToolChoiceType.Auto };
    public static ToolChoice None() => new() { Type = ToolChoiceType.None };
    public static ToolChoice Required() => new() { Type = ToolChoiceType.Required };
    public static ToolChoice Specific(string toolName) => new() { Type = ToolChoiceType.Specific, ToolName = toolName };
}

/// <summary>
/// 执行配置：重试、超时、退避等，统一模型调用语义。
/// </summary>
public class ExecutionConfig
{
    /// <summary>最大重试次数</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>重试间隔（固定间隔，未启用指数退避时使用）</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>单次调用超时</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>是否指数退避</summary>
    public bool ExponentialBackoff { get; set; } = true;

    /// <summary>指数退避初始间隔</summary>
    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>指数退避最大间隔</summary>
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>退避倍数（每次重试间隔乘以该值）</summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// 判断某个异常是否应触发重试。返回 true 表示重试，返回 false 或 null 表示不重试。
    /// 未设置时默认对所有异常重试。
    /// </summary>
    public Func<System.Exception, bool>? RetryOn { get; set; }
}

/// <summary>
/// 生成选项
/// Generation options for LLM requests
/// </summary>
public class GenerateOptions
{
    /// <summary>执行配置（重试/超时/退避）</summary>
    public ExecutionConfig? ExecutionConfig { get; set; }

    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
    public string? ModelName { get; set; }
    public bool? Stream { get; set; }
    public double? Temperature { get; set; }
    public int? MaxTokens { get; set; }
    public int? MaxCompletionTokens { get; set; }
    public double? TopP { get; set; }
    public int? TopK { get; set; }
    public double? FrequencyPenalty { get; set; }
    public double? PresencePenalty { get; set; }
    public List<string>? Stop { get; set; }
    public int? Seed { get; set; }
    public int? ThinkingBudget { get; set; }
    public string? ReasoningEffort { get; set; }
    public Model.CachePolicy? CacheControl { get; set; }
    public bool? ParallelToolCalls { get; set; }
    public ResponseFormat? ResponseFormat { get; set; }
    public ToolChoice? ToolChoice { get; set; }

    /// <summary>额外的请求体参数（提供商特定）</summary>
    public Dictionary<string, object>? AdditionalBodyParams { get; set; }

    /// <summary>额外的请求头</summary>
    public Dictionary<string, string>? AdditionalHeaders { get; set; }

    /// <summary>额外的查询参数</summary>
    public Dictionary<string, string>? AdditionalQueryParams { get; set; }

    /// <summary>
    /// 合并两个 GenerateOptions：primary 优先，fallback 作为默认
    /// </summary>
    public static GenerateOptions Merge(GenerateOptions? primary, GenerateOptions? fallback)
    {
        var result = new GenerateOptions();
        if (fallback != null)
        {
            result.ApiKey = fallback.ApiKey;
            result.BaseUrl = fallback.BaseUrl;
            result.ModelName = fallback.ModelName;
            result.Stream = fallback.Stream;
            result.Temperature = fallback.Temperature;
            result.MaxTokens = fallback.MaxTokens;
            result.MaxCompletionTokens = fallback.MaxCompletionTokens;
            result.TopP = fallback.TopP;
            result.TopK = fallback.TopK;
            result.FrequencyPenalty = fallback.FrequencyPenalty;
            result.PresencePenalty = fallback.PresencePenalty;
            result.Stop = fallback.Stop;
            result.Seed = fallback.Seed;
            result.ThinkingBudget = fallback.ThinkingBudget;
            result.ReasoningEffort = fallback.ReasoningEffort;
            result.CacheControl = fallback.CacheControl;
            result.ParallelToolCalls = fallback.ParallelToolCalls;
            result.ResponseFormat = fallback.ResponseFormat;
            result.ToolChoice = fallback.ToolChoice;
            result.ExecutionConfig = fallback.ExecutionConfig;
            result.AdditionalHeaders = fallback.AdditionalHeaders;
            result.AdditionalBodyParams = fallback.AdditionalBodyParams;
            result.AdditionalQueryParams = fallback.AdditionalQueryParams;
        }
        if (primary != null)
        {
            if (primary.ApiKey != null) result.ApiKey = primary.ApiKey;
            if (primary.BaseUrl != null) result.BaseUrl = primary.BaseUrl;
            if (primary.ModelName != null) result.ModelName = primary.ModelName;
            if (primary.Stream != null) result.Stream = primary.Stream;
            if (primary.Temperature != null) result.Temperature = primary.Temperature;
            if (primary.MaxTokens != null) result.MaxTokens = primary.MaxTokens;
            if (primary.MaxCompletionTokens != null) result.MaxCompletionTokens = primary.MaxCompletionTokens;
            if (primary.TopP != null) result.TopP = primary.TopP;
            if (primary.TopK != null) result.TopK = primary.TopK;
            if (primary.FrequencyPenalty != null) result.FrequencyPenalty = primary.FrequencyPenalty;
            if (primary.PresencePenalty != null) result.PresencePenalty = primary.PresencePenalty;
            if (primary.Stop != null) result.Stop = primary.Stop;
            if (primary.Seed != null) result.Seed = primary.Seed;
            if (primary.ThinkingBudget != null) result.ThinkingBudget = primary.ThinkingBudget;
            if (primary.ReasoningEffort != null) result.ReasoningEffort = primary.ReasoningEffort;
            if (primary.CacheControl != null) result.CacheControl = primary.CacheControl;
            if (primary.ParallelToolCalls != null) result.ParallelToolCalls = primary.ParallelToolCalls;
            if (primary.ResponseFormat != null) result.ResponseFormat = primary.ResponseFormat;
            if (primary.ToolChoice != null) result.ToolChoice = primary.ToolChoice;
            if (primary.ExecutionConfig != null) result.ExecutionConfig = primary.ExecutionConfig;
            if (primary.AdditionalHeaders != null) result.AdditionalHeaders = primary.AdditionalHeaders;
            if (primary.AdditionalBodyParams != null) result.AdditionalBodyParams = primary.AdditionalBodyParams;
            if (primary.AdditionalQueryParams != null) result.AdditionalQueryParams = primary.AdditionalQueryParams;
        }
        return result;
    }
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
