using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentScope.Core.Formatter.OpenAI.Dto;
using AgentScope.Core.Message;

namespace AgentScope.Core.Formatter.OpenAI;

/// <summary>
/// OpenAI Formatter 抽象基类
/// OpenAI Formatter abstract base class
/// 
/// 提供通用的格式化逻辑和参数处理
/// Provides common formatting logic and parameter handling
/// 
/// Java参考: io.agentscope.core.formatter.openai.OpenAIBaseFormatter
/// </summary>
public abstract class OpenAIBaseFormatter
{
    /// <summary>
    /// 模型名称
    /// Model name
    /// </summary>
    protected string ModelName { get; set; }

    /// <summary>
    /// 构造函数
    /// Constructor
    /// </summary>
    /// <param name="modelName">模型名称 / Model name</param>
    protected OpenAIBaseFormatter(string modelName)
    {
        ModelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
    }

    /// <summary>
    /// 格式化消息为OpenAI请求
    /// Format messages to OpenAI request
    /// </summary>
    /// <param name="messages">消息列表 / Message list</param>
    /// <param name="options">生成选项 / Generation options</param>
    /// <returns>OpenAI请求对象 / OpenAI request object</returns>
    public virtual OpenAIRequest Format(List<Msg> messages, GenerateOptions? options = null)
    {
        if (messages == null || messages.Count == 0)
        {
            throw new ArgumentException("Messages cannot be null or empty", nameof(messages));
        }

        // 转换消息
        // Convert messages
        var openAIMessages = new List<OpenAIMessage>();
        foreach (var msg in messages)
        {
            var openAIMsg = OpenAIMessageConverter.ConvertToMessage(msg);
            openAIMessages.Add(openAIMsg);
        }

        // 构建请求
        // Build request
        var request = new OpenAIRequest
        {
            Model = ModelName,
            Messages = openAIMessages
        };

        // 应用选项
        // Apply options
        if (options != null)
        {
            ApplyOptions(request, options);
        }

        return request;
    }

    /// <summary>
    /// 应用生成选项到请求
    /// Apply generation options to request
    /// </summary>
    protected virtual void ApplyOptions(OpenAIRequest request, GenerateOptions options)
    {
        // 基础参数
        // Basic parameters
        if (options.Temperature.HasValue)
        {
            request.Temperature = options.Temperature.Value;
        }

        if (options.TopP.HasValue)
        {
            request.TopP = options.TopP.Value;
        }

        if (options.MaxTokens.HasValue)
        {
            request.MaxTokens = options.MaxTokens;
        }

        if (options.MaxCompletionTokens.HasValue)
        {
            request.MaxCompletionTokens = options.MaxCompletionTokens;
        }

        if (options.FrequencyPenalty.HasValue)
        {
            request.FrequencyPenalty = options.FrequencyPenalty.Value;
        }

        if (options.PresencePenalty.HasValue)
        {
            request.PresencePenalty = options.PresencePenalty.Value;
        }

        if (options.Seed.HasValue)
        {
            request.Seed = options.Seed;
        }

        // Stop sequences
        if (options.Stop != null && options.Stop.Count > 0)
        {
            request.Stop = options.Stop;
        }

        // 流式输出
        // Streaming
        if (options.Stream.HasValue)
        {
            request.Stream = options.Stream.Value;
        }

        // 响应格式
        // Response format
        if (options.ResponseFormat != null)
        {
            request.ResponseFormat = options.ResponseFormat;
        }

        // o1系列特定参数
        // o1 series specific parameters
        if (!string.IsNullOrEmpty(options.ReasoningEffort))
        {
            request.ReasoningEffort = options.ReasoningEffort;
        }

        if (options.IncludeReasoning.HasValue)
        {
            request.IncludeReasoning = options.IncludeReasoning.Value;
        }

        // 工具相关
        // Tool related
        if (options.Tools != null && options.Tools.Count > 0)
        {
            request.Tools = ConvertTools(options.Tools);
        }

        if (options.ToolChoice != null)
        {
            request.ToolChoice = ConvertToolChoice(options.ToolChoice);
        }
    }

    /// <summary>
    /// 转换工具模式为OpenAI格式
    /// Convert tool schemas to OpenAI format
    /// </summary>
    protected virtual List<OpenAITool> ConvertTools(List<ToolSchema> tools)
    {
        var openAITools = new List<OpenAITool>();

        foreach (var tool in tools)
        {
            var openAITool = new OpenAITool
            {
                Type = "function",
                Function = new OpenAIToolFunction
                {
                    Name = tool.Name ?? string.Empty,
                    Description = tool.Description,
                    Parameters = tool.Parameters,
                    Strict = tool.Strict
                }
            };
            openAITools.Add(openAITool);
        }

        return openAITools;
    }

    /// <summary>
    /// 转换工具选择配置
    /// Convert tool choice configuration
    /// </summary>
    protected virtual object ConvertToolChoice(object toolChoice)
    {
        // 如果是字符串（auto, none, required）
        // If string (auto, none, required)
        if (toolChoice is string str)
        {
            return str;
        }

        // 如果是工具选择对象
        // If tool choice object
        if (toolChoice is ToolChoice tc && !string.IsNullOrEmpty(tc.FunctionName))
        {
            return new OpenAIToolChoice
            {
                Type = "function",
                Function = new OpenAIToolChoiceFunction
                {
                    Name = tc.FunctionName
                }
            };
        }

        return "auto"; // 默认值 / Default value
    }
}

/// <summary>
/// 生成选项
/// Generation options
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
    /// 最大token数（旧参数）
    /// Maximum tokens (deprecated parameter)
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// 最大完成token数
    /// Maximum completion tokens
    /// </summary>
    public int? MaxCompletionTokens { get; set; }

    /// <summary>
    /// 频率惩罚 (-2 to 2)
    /// Frequency penalty (-2 to 2)
    /// </summary>
    public double? FrequencyPenalty { get; set; }

    /// <summary>
    /// 存在惩罚 (-2 to 2)
    /// Presence penalty (-2 to 2)
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
    /// 是否流式输出
    /// Whether to stream
    /// </summary>
    public bool? Stream { get; set; }

    /// <summary>
    /// 响应格式
    /// Response format
    /// </summary>
    public ResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// 推理努力程度（o1系列）：low, medium, high
    /// Reasoning effort (o1 series): low, medium, high
    /// </summary>
    public string? ReasoningEffort { get; set; }

    /// <summary>
    /// 是否包含推理内容（o1系列）
    /// Whether to include reasoning (o1 series)
    /// </summary>
    public bool? IncludeReasoning { get; set; }

    /// <summary>
    /// 工具列表
    /// Tools list
    /// </summary>
    public List<ToolSchema>? Tools { get; set; }

    /// <summary>
    /// 工具选择
    /// Tool choice
    /// </summary>
    public object? ToolChoice { get; set; }
}

/// <summary>
/// 工具模式
/// Tool schema
/// </summary>
public class ToolSchema
{
    /// <summary>
    /// 工具名称
    /// Tool name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 工具描述
    /// Tool description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 参数模式（JSON Schema）
    /// Parameter schema (JSON Schema)
    /// </summary>
    public object? Parameters { get; set; }

    /// <summary>
    /// 是否严格模式
    /// Strict mode
    /// </summary>
    public bool? Strict { get; set; }
}

/// <summary>
/// 工具选择
/// Tool choice
/// </summary>
public class ToolChoice
{
    /// <summary>
    /// 函数名称
    /// Function name
    /// </summary>
    public string? FunctionName { get; set; }
}

/// <summary>
/// 响应格式
/// Response format
/// </summary>
public class ResponseFormat
{
    /// <summary>
    /// 格式类型：text, json_object, json_schema
    /// Format type: text, json_object, json_schema
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// JSON Schema（当type为json_schema时）
    /// JSON Schema (when type is json_schema)
    /// </summary>
    public JsonSchema? JsonSchema { get; set; }
}

/// <summary>
/// JSON Schema定义
/// JSON Schema definition
/// </summary>
public class JsonSchema
{
    /// <summary>
    /// Schema名称
    /// Schema name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Schema定义
    /// Schema definition
    /// </summary>
    public object? Schema { get; set; }

    /// <summary>
    /// 是否严格模式
    /// Strict mode
    /// </summary>
    public bool? Strict { get; set; }
}
