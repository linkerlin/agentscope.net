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
/// Formatter interface for converting between AgentScope and provider-specific formats.
/// Formatter 接口，用于在 AgentScope 和特定提供商格式之间进行转换
///
/// Responsibilities:
/// 职责：
/// 1. Converting Msg objects to provider-specific request format
///    将 Msg 对象转换为提供商特定的请求格式
/// 2. Converting provider-specific responses back to AgentScope ChatResponse
///    将提供商特定的响应转换回 AgentScope ChatResponse
/// 3. Applying generation options to provider-specific request builders
///    应用生成选项到提供商特定的请求构建器
/// 4. Applying tool schemas to provider-specific request builders
///    应用工具模式到提供商特定的请求构建器
/// </summary>
/// <typeparam name="TRequest">Provider-specific request message type / 提供商特定的请求消息类型</typeparam>
/// <typeparam name="TResponse">Provider-specific response type / 提供商特定的响应类型</typeparam>
/// <typeparam name="TParams">Provider-specific request parameters builder type / 提供商特定的请求参数构建器类型</typeparam>
public interface IFormatter<TRequest, TResponse, TParams>
{
    /// <summary>
    /// Format AgentScope messages to provider-specific request format.
    /// 将 AgentScope 消息格式化为提供商特定的请求格式
    /// </summary>
    /// <param name="messages">AgentScope messages / AgentScope 消息列表</param>
    /// <returns>Provider-specific formatted messages / 提供商特定的格式化消息列表</returns>
    List<TRequest> Format(List<Msg> messages);

    /// <summary>
    /// Parse provider-specific response to AgentScope ChatResponse.
    /// 解析提供商特定的响应为 AgentScope ChatResponse
    /// </summary>
    /// <param name="response">Provider-specific response / 提供商特定的响应</param>
    /// <param name="startTime">Request start time / 请求开始时间</param>
    /// <returns>AgentScope ChatResponse / AgentScope 聊天响应</returns>
    ModelResponse ParseResponse(TResponse response, DateTime startTime);

    /// <summary>
    /// Apply generation options to provider-specific request parameters.
    /// 应用生成选项到提供商特定的请求参数
    /// </summary>
    /// <param name="paramsBuilder">Provider-specific parameters builder / 提供商特定的参数构建器</param>
    /// <param name="options">Generation options / 生成选项</param>
    /// <param name="defaultOptions">Default generation options / 默认生成选项</param>
    void ApplyOptions(TParams paramsBuilder, GenerateOptions? options, GenerateOptions? defaultOptions);

    /// <summary>
    /// Apply tool schemas to provider-specific request parameters.
    /// 应用工具模式到提供商特定的请求参数
    /// </summary>
    /// <param name="paramsBuilder">Provider-specific parameters builder / 提供商特定的参数构建器</param>
    /// <param name="tools">Tool schemas / 工具模式列表</param>
    void ApplyTools(TParams paramsBuilder, List<ToolSchema>? tools);

    /// <summary>
    /// Apply tool schemas with provider compatibility handling.
    /// 应用工具模式到提供商特定的请求参数（带提供商兼容性处理）
    /// </summary>
    /// <param name="paramsBuilder">Provider-specific parameters builder / 提供商特定的参数构建器</param>
    /// <param name="tools">Tool schemas / 工具模式列表</param>
    /// <param name="baseUrl">Base URL for the provider / 提供商的基础 URL</param>
    /// <param name="modelName">Model name / 模型名称</param>
    void ApplyTools(TParams paramsBuilder, List<ToolSchema>? tools, string? baseUrl, string? modelName)
    {
        // 默认实现：委托给简单方法
        // Default implementation: delegate to the simpler method
        ApplyTools(paramsBuilder, tools);
    }
}

/// <summary>
/// Tool choice type enumeration.
/// 工具选择类型
/// </summary>
public enum ToolChoiceType
{
    /// <summary>Let the model decide automatically / 让模型自动选择</summary>
    Auto,

    /// <summary>Disable tool usage / 禁用工具使用</summary>
    None,

    /// <summary>Force tool usage / 强制使用工具</summary>
    Required,

    /// <summary>Use a specific tool / 使用指定的工具</summary>
    Specific
}

/// <summary>
/// Tool choice configuration for LLM requests.
/// 工具选择配置
/// </summary>
public class ToolChoice
{
    /// <summary>Type of tool choice / 工具选择类型</summary>
    public ToolChoiceType Type { get; set; }

    /// <summary>Specific tool name (used when Type is Specific) / 指定工具名称（Type 为 Specific 时使用）</summary>
    public string? ToolName { get; set; }

    /// <summary>Creates an Auto tool choice / 创建自动选择</summary>
    public static ToolChoice Auto() => new() { Type = ToolChoiceType.Auto };

    /// <summary>Creates a None tool choice (disables tools) / 创建无工具选择</summary>
    public static ToolChoice None() => new() { Type = ToolChoiceType.None };

    /// <summary>Creates a Required tool choice / 创建强制工具选择</summary>
    public static ToolChoice Required() => new() { Type = ToolChoiceType.Required };

    /// <summary>Creates a Specific tool choice for the given tool / 创建指定工具选择</summary>
    public static ToolChoice Specific(string toolName) => new() { Type = ToolChoiceType.Specific, ToolName = toolName };
}

/// <summary>
/// Execution configuration: retry, timeout, backoff, etc. Unifies model call semantics.
/// 执行配置：重试、超时、退避等，统一模型调用语义。
/// </summary>
public class ExecutionConfig
{
    /// <summary>Maximum number of retries / 最大重试次数</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Retry interval (used when exponential backoff is disabled) / 重试间隔（未启用指数退避时使用）</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Timeout for a single call / 单次调用超时</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Whether to use exponential backoff / 是否指数退避</summary>
    public bool ExponentialBackoff { get; set; } = true;

    /// <summary>Initial backoff interval / 指数退避初始间隔</summary>
    public TimeSpan InitialBackoff { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Maximum backoff interval / 指数退避最大间隔</summary>
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Backoff multiplier (retry interval multiplied by this each retry) / 退避倍数（每次重试间隔乘以该值）</summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Determines whether an exception should trigger a retry. Returns true to retry, false/null to not retry.
    /// 判断某个异常是否应触发重试。返回 true 表示重试，返回 false 或 null 表示不重试。
    /// When not set, all exceptions trigger retry by default.
    /// 未设置时默认对所有异常重试。
    /// </summary>
    public Func<System.Exception, bool>? RetryOn { get; set; }
}

/// <summary>
/// Generation options for LLM requests.
/// 生成选项
/// </summary>
public class GenerateOptions
{
    /// <summary>Execution config (retry/timeout/backoff) / 执行配置（重试/超时/退避）</summary>
    public ExecutionConfig? ExecutionConfig { get; set; }

    /// <summary>API key for authentication / API 认证密钥</summary>
    public string? ApiKey { get; set; }

    /// <summary>Base URL for the API / API 基础地址</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Model name / 模型名称</summary>
    public string? ModelName { get; set; }

    /// <summary>Whether to stream the response / 是否流式输出</summary>
    public bool? Stream { get; set; }

    /// <summary>Temperature parameter (0.0-2.0) / 温度参数</summary>
    public double? Temperature { get; set; }

    /// <summary>Maximum tokens to generate / 最大生成 token 数</summary>
    public int? MaxTokens { get; set; }

    /// <summary>Maximum completion tokens / 最大补全 token 数</summary>
    public int? MaxCompletionTokens { get; set; }

    /// <summary>Top-p sampling parameter / Top-p 采样参数</summary>
    public double? TopP { get; set; }

    /// <summary>Top-k sampling parameter / Top-k 采样参数</summary>
    public int? TopK { get; set; }

    /// <summary>Frequency penalty / 频率惩罚</summary>
    public double? FrequencyPenalty { get; set; }

    /// <summary>Presence penalty / 存在惩罚</summary>
    public double? PresencePenalty { get; set; }

    /// <summary>Stop sequences / 停止序列</summary>
    public List<string>? Stop { get; set; }

    /// <summary>Random seed / 随机种子</summary>
    public int? Seed { get; set; }

    /// <summary>Thinking budget tokens (extended thinking) / 思考预算 token 数</summary>
    public int? ThinkingBudget { get; set; }

    /// <summary>Reasoning effort level / 推理努力级别</summary>
    public string? ReasoningEffort { get; set; }

    /// <summary>Cache control policy / 缓存控制策略</summary>
    public Model.CachePolicy? CacheControl { get; set; }

    /// <summary>Whether to allow parallel tool calls / 是否允许并行工具调用</summary>
    public bool? ParallelToolCalls { get; set; }

    /// <summary>Response format (text, JSON, JSON Schema) / 响应格式</summary>
    public ResponseFormat? ResponseFormat { get; set; }

    /// <summary>Tool choice configuration / 工具选择配置</summary>
    public ToolChoice? ToolChoice { get; set; }

    /// <summary>Additional request body parameters (provider-specific) / 额外的请求体参数（提供商特定）</summary>
    public Dictionary<string, object>? AdditionalBodyParams { get; set; }

    /// <summary>Additional request headers / 额外的请求头</summary>
    public Dictionary<string, string>? AdditionalHeaders { get; set; }

    /// <summary>Additional query parameters / 额外的查询参数</summary>
    public Dictionary<string, string>? AdditionalQueryParams { get; set; }

    /// <summary>
    /// Merge two GenerateOptions: primary takes precedence, fallback provides defaults.
    /// 合并两个 GenerateOptions：primary 优先，fallback 作为默认
    /// </summary>
    /// <param name="primary">Primary options (higher priority) / 优先选项</param>
    /// <param name="fallback">Fallback options (lower priority) / 备选选项</param>
    /// <returns>Merged GenerateOptions / 合并后的选项</returns>
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
/// Tool schema for function calling.
/// 工具模式
/// </summary>
public class ToolSchema
{
    /// <summary>Tool name / 工具名称</summary>
    public string Name { get; set; } = "";

    /// <summary>Tool description / 工具描述</summary>
    public string? Description { get; set; }

    /// <summary>Tool parameters (JSON Schema format) / 工具参数（JSON Schema 格式）</summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>Whether to enforce strict schema adherence / 是否严格遵循 schema</summary>
    public bool? Strict { get; set; }
}

/// <summary>
/// Response format configuration for structured output.
/// 响应格式配置
/// </summary>
public class ResponseFormat
{
    /// <summary>
    /// Response format type: "text", "json_object", "json_schema".
    /// 响应格式类型："text", "json_object", "json_schema"
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// JSON Schema specification (only used for json_schema type).
    /// JSON Schema 规范（仅用于 json_schema 类型）
    /// </summary>
    public JsonSchema? JsonSchema { get; set; }

    /// <summary>Creates a text response format / 创建文本响应格式</summary>
    public static ResponseFormat Text() => new() { Type = "text" };

    /// <summary>Creates a JSON object response format / 创建 JSON 对象响应格式</summary>
    public static ResponseFormat JsonObject() => new() { Type = "json_object" };

    /// <summary>Creates a JSON Schema response format / 创建 JSON Schema 响应格式</summary>
    public static ResponseFormat WithJsonSchema(JsonSchema schema) =>
        new() { Type = "json_schema", JsonSchema = schema };
}

/// <summary>
/// JSON Schema definition for structured output.
/// JSON Schema 定义
/// </summary>
public class JsonSchema
{
    /// <summary>Schema name / Schema 名称</summary>
    public string Name { get; set; } = "";

    /// <summary>Schema definition (JSON Schema object) / Schema 定义（JSON Schema 对象）</summary>
    public Dictionary<string, object>? Schema { get; set; }

    /// <summary>Whether to enforce strict schema adherence / 是否严格遵循 schema</summary>
    public bool? Strict { get; set; }
}

/// <summary>
/// Formatter exception.
/// Formatter 异常
/// </summary>
public class FormatterException : AgentScope.Core.Exception.AgentScopeException
{
    /// <summary>
    /// Creates a new instance of FormatterException.
    /// 创建 FormatterException 的新实例
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    public FormatterException(string message) : base(message) { }

    /// <summary>
    /// Creates a new instance of FormatterException with inner exception.
    /// 使用内部异常创建 FormatterException 的新实例
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    /// <param name="innerException">Inner exception / 内部异常</param>
    public FormatterException(string message, System.Exception innerException)
        : base(message, innerException) { }
}
