using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentScope.Core.Formatter.OpenAI.Dto;
using AgentScope.Core.Message;

namespace AgentScope.Core.Formatter.OpenAI;

/// <summary>
/// OpenAI Chat Completions Formatter
/// OpenAI Chat Completions 格式化器
/// 
/// 实现OpenAI Chat Completions API的消息格式化
/// Implements message formatting for OpenAI Chat Completions API
/// 
/// Java参考: io.agentscope.core.formatter.openai.OpenAIChatFormatter
/// </summary>
public class OpenAIChatFormatter : OpenAIBaseFormatter
{
    /// <summary>
    /// 构造函数
    /// Constructor
    /// </summary>
    /// <param name="modelName">模型名称 / Model name (e.g., "gpt-4", "gpt-3.5-turbo")</param>
    public OpenAIChatFormatter(string modelName) : base(modelName)
    {
    }

    /// <summary>
    /// 格式化消息为OpenAI Chat Completions请求
    /// Format messages to OpenAI Chat Completions request
    /// </summary>
    /// <param name="messages">消息列表 / Message list</param>
    /// <param name="options">生成选项 / Generation options</param>
    /// <returns>OpenAI Chat Completions请求对象 / OpenAI Chat Completions request object</returns>
    public override OpenAIRequest Format(List<Msg> messages, GenerateOptions? options = null)
    {
        // 调用基类的Format方法
        // Call base class Format method
        var request = base.Format(messages, options);

        // Chat Completions特定的处理
        // Chat Completions specific handling
        // (目前没有特殊处理，但预留扩展空间)
        // (No special handling currently, but reserved for future extensions)

        return request;
    }

    /// <summary>
    /// 格式化单个消息
    /// Format single message
    /// </summary>
    /// <param name="message">消息 / Message</param>
    /// <param name="options">生成选项 / Generation options</param>
    /// <returns>OpenAI请求对象 / OpenAI request object</returns>
    public OpenAIRequest FormatSingle(Msg message, GenerateOptions? options = null)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        return Format(new List<Msg> { message }, options);
    }

    /// <summary>
    /// 解析OpenAI响应
    /// Parse OpenAI response
    /// </summary>
    /// <param name="response">OpenAI响应 / OpenAI response</param>
    /// <returns>解析后的响应 / Parsed response</returns>
    public ParsedResponse Parse(OpenAIResponse response)
    {
        return OpenAIResponseParser.ParseResponse(response);
    }

    /// <summary>
    /// 格式化并解析（便捷方法）
    /// Format and parse (convenience method)
    /// </summary>
    /// <param name="messages">消息列表 / Message list</param>
    /// <param name="options">生成选项 / Generation options</param>
    /// <param name="apiCall">API调用函数 / API call function</param>
    /// <returns>解析后的响应 / Parsed response</returns>
    public async Task<ParsedResponse> FormatAndCallAsync(
        List<Msg> messages,
        GenerateOptions? options,
        Func<OpenAIRequest, Task<OpenAIResponse>> apiCall)
    {
        if (apiCall == null)
        {
            throw new ArgumentNullException(nameof(apiCall));
        }

        // 格式化请求
        // Format request
        var request = Format(messages, options);

        // 调用API
        // Call API
        var response = await apiCall(request);

        // 解析响应
        // Parse response
        return Parse(response);
    }

    /// <summary>
    /// 创建带工具的Formatter
    /// Create formatter with tools
    /// </summary>
    /// <param name="modelName">模型名称 / Model name</param>
    /// <param name="tools">工具列表 / Tools list</param>
    /// <param name="toolChoice">工具选择 / Tool choice</param>
    /// <returns>配置好的Formatter / Configured formatter</returns>
    public static OpenAIChatFormatter WithTools(
        string modelName,
        List<ToolSchema> tools,
        object? toolChoice = null)
    {
        var formatter = new OpenAIChatFormatter(modelName);
        
        // 注意：工具通过GenerateOptions传递
        // Note: Tools are passed through GenerateOptions
        // 这里只是创建Formatter实例
        // Here we just create the Formatter instance
        
        return formatter;
    }

    /// <summary>
    /// 创建带JSON输出的Formatter
    /// Create formatter with JSON output
    /// </summary>
    /// <param name="modelName">模型名称 / Model name</param>
    /// <param name="jsonSchema">JSON Schema / JSON Schema (optional)</param>
    /// <returns>配置好的Formatter / Configured formatter</returns>
    public static OpenAIChatFormatter WithJsonOutput(
        string modelName,
        JsonSchema? jsonSchema = null)
    {
        var formatter = new OpenAIChatFormatter(modelName);
        
        // 注意：JSON输出格式通过GenerateOptions传递
        // Note: JSON output format is passed through GenerateOptions
        // 这里只是创建Formatter实例
        // Here we just create the Formatter instance
        
        return formatter;
    }
}
