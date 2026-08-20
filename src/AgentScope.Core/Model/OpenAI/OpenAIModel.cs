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
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Formatter;
using AgentScope.Core.Formatter.OpenAI;
using AgentScope.Core.Message;
using AgentScope.Core.Model.Transport;

using GenerateOptions = AgentScope.Core.Formatter.OpenAI.GenerateOptions;

namespace AgentScope.Core.Model.OpenAI;

/// <summary>
/// OpenAI Chat Model using native HTTP API for the AgentScope framework.
/// Supports both streaming and non-streaming chat completions, tool calling,
/// and automatic message format conversion via the OpenAIChatFormatter.
/// Corresponds to Java: io.agentscope.core.model.OpenAIChatModel
/// AgentScope 框架的 OpenAI 聊天模型，使用原生 HTTP API。
/// 支持流式和非流式聊天补全、工具调用，
/// 以及通过 OpenAIChatFormatter 自动进行消息格式转换。
/// 对应 Java: io.agentscope.core.model.OpenAIChatModel
/// </summary>
public class OpenAIModel : ModelBase, IStreamingChatModel
{
    /// <summary>
    /// HTTP client for communicating with the OpenAI-compatible API endpoint.
    /// 用于与 OpenAI 兼容 API 端点通信的 HTTP 客户端。
    /// </summary>
    private readonly OpenAIClient _client;

    /// <summary>
    /// Formatter for converting AgentScope messages to OpenAI request format and parsing responses.
    /// 用于将 AgentScope 消息转换为 OpenAI 请求格式并解析响应的格式化器。
    /// </summary>
    private readonly OpenAIChatFormatter _formatter;

    /// <summary>
    /// API key for authentication (optional, can be set via environment variable).
    /// 用于身份验证的 API 密钥（可选，可通过环境变量设置）。
    /// </summary>
    private readonly string? _apiKey;

    /// <summary>
    /// Custom base URL for the API endpoint (optional, defaults to https://api.openai.com).
    /// API 端点的自定义基础 URL（可选，默认为 https://api.openai.com）。
    /// </summary>
    private readonly string? _baseUrl;

    /// <summary>
    /// The model identifier (e.g., "gpt-4o", "gpt-4-turbo").
    /// 模型标识符（例如 "gpt-4o"、"gpt-4-turbo"）。
    /// </summary>
    private readonly string _modelName;

    /// <summary>
    /// Default generation options applied to all requests (can be overridden per-request).
    /// 应用于所有请求的默认生成选项（可在每次请求时覆盖）。
    /// </summary>
    private readonly GenerateOptions? _defaultOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIModel"/> class.
    /// 初始化 <see cref="OpenAIModel"/> 类的新实例。
    /// </summary>
    /// <param name="modelName">Model identifier (e.g., "gpt-4o") / 模型标识符。</param>
    /// <param name="apiKey">API key for authentication / API 密钥。</param>
    /// <param name="baseUrl">Custom base URL for the API / API 的自定义基础 URL。</param>
    /// <param name="client">Custom HTTP client / 自定义 HTTP 客户端。</param>
    /// <param name="formatter">Custom message formatter / 自定义消息格式化器。</param>
    /// <param name="defaultOptions">Default generation options / 默认生成选项。</param>
    public OpenAIModel(
        string modelName,
        string? apiKey = null,
        string? baseUrl = null,
        OpenAIClient? client = null,
        OpenAIChatFormatter? formatter = null,
        GenerateOptions? defaultOptions = null)
        : base(modelName)
    {
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
        _apiKey = apiKey;
        _baseUrl = baseUrl;
        _client = client ?? new OpenAIClient();
        _formatter = formatter ?? new OpenAIChatFormatter(modelName);
        _defaultOptions = defaultOptions;
    }

    /// <inheritdoc />
    public override IObservable<ModelResponse> Generate(ModelRequest request)
    {
        // Wrap the async method as an observable for Rx-style consumption
        // 将异步方法包装为可观察对象以支持 Rx 风格消费
        return Observable.FromAsync(async () =>
        {
            var response = await GenerateAsync(request);
            return response;
        });
    }

    /// <inheritdoc />
    public override async Task<ModelResponse> GenerateAsync(ModelRequest request)
    {
        var messages = request.Messages;
        var options = MergeOptions(ConvertOptions(request.Options), _defaultOptions);
        var startTime = DateTime.UtcNow;

        // Step 1: Format AgentScope messages into OpenAI API request format
        // 步骤 1：将 AgentScope 消息格式化为 OpenAI API 请求格式
        var openaiRequest = _formatter.Format(messages, options);

        // Step 2: Send the request to the OpenAI API endpoint
        // 步骤 2：向 OpenAI API 端点发送请求
        var response = await _client.CallAsync(_apiKey, _baseUrl, openaiRequest);

        // Step 3: Parse the API response and convert to ChatResponse
        // 步骤 3：解析 API 响应并转换为 ChatResponse
        var parsedResponse = _formatter.Parse(response);
        var chatResponse = ConvertToChatResponse(parsedResponse);
        return chatResponse;
    }

    /// <summary>
    /// Generates a streaming response from the OpenAI model.
    /// Each chunk from the SSE stream is parsed and yielded as a ChatResponse.
    /// 从 OpenAI 模型生成流式响应。
    /// SSE 流中的每个块都会被解析并作为 ChatResponse 生成。
    /// </summary>
    /// <param name="messages">List of conversation messages / 对话消息列表。</param>
    /// <param name="options">Optional generation options / 可选的生成选项。</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌。</param>
    /// <returns>Async enumerable of ChatResponse chunks / ChatResponse 块的异步可枚举序列。</returns>
    public async IAsyncEnumerable<ChatResponse> GenerateStreamAsync(
        List<Msg> messages,
        GenerateOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var mergedOptions = MergeOptions(options, _defaultOptions);
        mergedOptions ??= new GenerateOptions();
        mergedOptions.Stream = true;

        // Format messages into OpenAI request format with streaming enabled
        // 将消息格式化为启用流式的 OpenAI 请求格式
        var openaiRequest = _formatter.Format(messages, mergedOptions);

        // Stream API call - process each SSE chunk as it arrives
        // 流式 API 调用 - 处理每个到达的 SSE 块
        await foreach (var chunk in _client.StreamAsync(_apiKey, _baseUrl, openaiRequest, cancellationToken))
        {
            var parsedResponse = _formatter.Parse(chunk);
            if (parsedResponse != null)
            {
                var chatResponse = ConvertToChatResponse(parsedResponse);
                yield return chatResponse;
            }
        }
    }

    /// <summary>
    /// Generates a streaming response with default options.
    /// 使用默认选项生成流式响应。
    /// </summary>
    public IAsyncEnumerable<ChatResponse> GenerateStreamAsync(
        List<Msg> messages,
        CancellationToken cancellationToken = default)
    {
        return GenerateStreamAsync(messages, options: null, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Merges provided options with default options, with provided options taking precedence.
    /// 合并提供的选项与默认选项，提供的选项优先。
    /// </summary>
    /// <param name="options">Request-specific options / 请求特定选项。</param>
    /// <param name="defaults">Default options / 默认选项。</param>
    /// <returns>Merged options or null if both are null / 合并后的选项，如果两者都为空则返回 null。</returns>
    private GenerateOptions? MergeOptions(GenerateOptions? options, GenerateOptions? defaults)
    {
        if (options == null) return defaults;
        if (defaults == null) return options;

        var merged = new GenerateOptions
        {
            Temperature = options.Temperature ?? defaults.Temperature,
            MaxTokens = options.MaxTokens ?? defaults.MaxTokens,
            TopP = options.TopP ?? defaults.TopP,

            FrequencyPenalty = options.FrequencyPenalty ?? defaults.FrequencyPenalty,
            PresencePenalty = options.PresencePenalty ?? defaults.PresencePenalty,
            Seed = options.Seed ?? defaults.Seed,
            ResponseFormat = options.ResponseFormat ?? defaults.ResponseFormat,
            Stop = options.Stop ?? defaults.Stop
        };

        return merged;
    }

    /// <summary>
    /// Converts a Dictionary&lt;string, object&gt; options map to a strongly-typed GenerateOptions.
    /// This allows dynamic option passing from middleware or configuration systems.
    /// 将 Dictionary&lt;string, object&gt; 选项字典转换为强类型的 GenerateOptions。
    /// 这允许从中间件或配置系统动态传递选项。
    /// </summary>
    /// <param name="options">Raw options dictionary / 原始选项字典。</param>
    /// <returns>Converted GenerateOptions or null / 转换后的 GenerateOptions 或 null。</returns>
    private GenerateOptions? ConvertOptions(Dictionary<string, object>? options)
    {
        if (options == null) return null;

        var result = new GenerateOptions();

        if (options.TryGetValue("temperature", out var temp) && temp is double tempValue)
            result.Temperature = tempValue;
        if (options.TryGetValue("maxTokens", out var maxTokens) && maxTokens is int maxTokensValue)
            result.MaxTokens = maxTokensValue;
        if (options.TryGetValue("topP", out var topP) && topP is double topPValue)
            result.TopP = topPValue;

        if (options.TryGetValue("frequencyPenalty", out var freqPenalty) && freqPenalty is double freqPenaltyValue)
            result.FrequencyPenalty = freqPenaltyValue;
        if (options.TryGetValue("presencePenalty", out var presPenalty) && presPenalty is double presPenaltyValue)
            result.PresencePenalty = presPenaltyValue;
        if (options.TryGetValue("seed", out var seed) && seed is int seedValue)
            result.Seed = seedValue;
        if (options.TryGetValue("stop", out var stop) && stop is List<string> stopValue)
            result.Stop = stopValue;
        if (options.TryGetValue("responseFormat", out var responseFormat) && responseFormat is Formatter.OpenAI.ResponseFormat formatValue)
            result.ResponseFormat = formatValue;

        return result;
    }

    /// <summary>
    /// Converts a ParsedResponse from the formatter into a ChatResponse for the AgentScope framework.
    /// Handles text content, tool calls, usage statistics, and reasoning content.
    /// 将格式化器的 ParsedResponse 转换为 AgentScope 框架的 ChatResponse。
    /// 处理文本内容、工具调用、使用统计和推理内容。
    /// </summary>
    /// <param name="parsed">Parsed response from the formatter / 来自格式化器的解析响应。</param>
    /// <returns>A ChatResponse ready for consumption / 准备消费的 ChatResponse。</returns>
    private ChatResponse ConvertToChatResponse(ParsedResponse parsed)
    {
        // 推理模型可能仅返回 reasoning 字段而 content 为空，此时兜底使用推理内容
        // Reasoning models may return only the reasoning field with empty content; fall back to it
        var text = string.IsNullOrWhiteSpace(parsed.TextContent)
            ? parsed.ReasoningContent
            : parsed.TextContent;

        var chatResponse = new ChatResponse
        {
            Id = parsed.Id,
            Model = parsed.Model,
            Content = text,
            Text = text, // Also set Text for ModelResponse compatibility / 同时设置 Text 以兼容 ModelResponse
            StopReason = parsed.FinishReason,
            Success = true
        };

        // Map token usage statistics if available
        // 映射令牌使用统计（如果可用）
        if (parsed.Usage != null)
        {
            chatResponse.Usage = new ChatUsage
            {
                InputTokens = parsed.Usage.PromptTokens,
                OutputTokens = parsed.Usage.CompletionTokens,
                TotalTokens = parsed.Usage.TotalTokens
            };
        }

        // Map tool calls if present in the response
        // 映射响应中的工具调用（如果存在）
        if (parsed.ToolCalls != null && parsed.ToolCalls.Count > 0)
        {
            chatResponse.ToolCalls = new List<ToolCallInfo>();
            foreach (var tc in parsed.ToolCalls)
            {
                chatResponse.ToolCalls.Add(new ToolCallInfo
                {
                    Id = tc.Id ?? string.Empty,
                    Name = tc.FunctionName ?? string.Empty,
                    Type = tc.Type,
                    Arguments = tc.FunctionArguments
                });
            }
        }

        // Store reasoning/thinking content in metadata (e.g., for DeepSeek-R1, OpenAI o1)
        // 将推理/思考内容存储在元数据中（例如 DeepSeek-R1、OpenAI o1）
        if (!string.IsNullOrEmpty(parsed.ReasoningContent))
        {
            chatResponse.Metadata ??= new Dictionary<string, object>();
            chatResponse.Metadata["thinking"] = parsed.ReasoningContent;
        }

        return chatResponse;
    }

    /// <summary>
    /// Creates a new Builder for fluent construction of OpenAIModel instances.
    /// 创建新的 Builder 以支持流畅构建 OpenAIModel 实例。
    /// </summary>
    /// <returns>A new Builder instance / 新的 Builder 实例。</returns>
    public static Builder CreateBuilder() => new();

    /// <summary>
    /// Fluent builder for <see cref="OpenAIModel"/> using the builder pattern.
    /// Allows configuring all model properties before construction.
    /// OpenAIModel 的流畅构建器，使用构建器模式。
    /// 允许在构造前配置所有模型属性。
    /// </summary>
    public class Builder
    {
        private string? _apiKey;
        private string? _modelName;
        private string? _baseUrl;
        private OpenAIClient? _client;
        private OpenAIChatFormatter? _formatter;
        private GenerateOptions? _defaultOptions;

        /// <summary>
        /// Sets the API key for authentication.
        /// 设置用于身份验证的 API 密钥。
        /// </summary>
        public Builder ApiKey(string apiKey)
        {
            _apiKey = apiKey;
            return this;
        }

        /// <summary>
        /// Sets the model name/identifier.
        /// 设置模型名称/标识符。
        /// </summary>
        public Builder ModelName(string modelName)
        {
            _modelName = modelName;
            return this;
        }

        /// <summary>
        /// Sets the custom base URL for the API endpoint.
        /// 设置 API 端点的自定义基础 URL。
        /// </summary>
        public Builder BaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl;
            return this;
        }

        /// <summary>
        /// Sets a custom HTTP client implementation.
        /// 设置自定义 HTTP 客户端实现。
        /// </summary>
        public Builder Client(OpenAIClient client)
        {
            _client = client;
            return this;
        }

        /// <summary>
        /// Sets a custom message formatter.
        /// 设置自定义消息格式化器。
        /// </summary>
        public Builder Formatter(OpenAIChatFormatter formatter)
        {
            _formatter = formatter;
            return this;
        }

        /// <summary>
        /// Sets default generation options.
        /// 设置默认生成选项。
        /// </summary>
        public Builder DefaultOptions(GenerateOptions options)
        {
            _defaultOptions = options;
            return this;
        }

        /// <summary>
        /// Builds the <see cref="OpenAIModel"/> instance with the configured properties.
        /// 使用已配置的属性构建 <see cref="OpenAIModel"/> 实例。
        /// </summary>
        /// <returns>A new <see cref="OpenAIModel"/> instance / 新的 <see cref="OpenAIModel"/> 实例。</returns>
        /// <exception cref="ArgumentException">Thrown when model name is not set / 当未设置模型名称时抛出。</exception>
        public OpenAIModel Build()
        {
            if (string.IsNullOrEmpty(_modelName))
            {
                throw new ArgumentException("Model name must be set / 必须设置模型名称");
            }

            return new OpenAIModel(
                _modelName,
                _apiKey,
                _baseUrl,
                _client,
                _formatter,
                _defaultOptions);
        }
    }
}
