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
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Formatter.Anthropic;
using AgentScope.Core.Formatter.Anthropic.Dto;
using AgentScope.Core.Message;

using GenerateOptions = AgentScope.Core.Formatter.Anthropic.GenerateOptions;

namespace AgentScope.Core.Model.Anthropic;

/// <summary>
/// Anthropic Claude Model using native HTTP API for the AgentScope framework.
/// Supports both streaming and non-streaming chat completions, tool calling,
/// and extended thinking (Claude 3.7 Sonnet+).
/// Uses the Anthropic Messages API (/v1/messages) with SSE-based streaming.
/// Corresponds to Java: io.agentscope.core.model.AnthropicChatModel
/// AgentScope 框架的 Anthropic Claude 模型，使用原生 HTTP API。
/// 支持流式和非流式聊天补全、工具调用，
/// 以及扩展思考功能（Claude 3.7 Sonnet+）。
/// 使用 Anthropic Messages API (/v1/messages) 和基于 SSE 的流式传输。
/// 对应 Java: io.agentscope.core.model.AnthropicChatModel
/// </summary>
public class AnthropicModel : ModelBase, IStreamingChatModel
{
    /// <summary>
    /// Default base URL for the Anthropic API.
    /// Anthropic API 的默认基础地址。
    /// </summary>
    public const string DefaultBaseUrl = "https://api.anthropic.com";

    /// <summary>
    /// Messages API endpoint path for the Anthropic API.
    /// Anthropic API 的消息 API 端点路径。
    /// </summary>
    public const string MessagesEndpoint = "/v1/messages";

    /// <summary>
    /// HTTP client for communicating with the Anthropic API.
    /// 用于与 Anthropic API 通信的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Formatter for converting AgentScope messages to Anthropic request format and parsing responses.
    /// 用于将 AgentScope 消息转换为 Anthropic 请求格式并解析响应的格式化器。
    /// </summary>
    private readonly AnthropicChatFormatter _formatter;

    /// <summary>
    /// API key for authentication (optional, falls back to ANTHROPIC_API_KEY env var).
    /// 用于身份验证的 API 密钥（可选，未提供则读取环境变量 ANTHROPIC_API_KEY）。
    /// </summary>
    private readonly string? _apiKey;

    /// <summary>
    /// Custom base URL for the API endpoint (optional, defaults to https://api.anthropic.com).
    /// API 端点的自定义基础 URL（可选，默认为 https://api.anthropic.com）。
    /// </summary>
    private readonly string? _baseUrl;

    /// <summary>
    /// The model identifier (e.g., "claude-3-5-sonnet-20241022", "claude-3-opus-20240229").
    /// 模型标识符（例如 "claude-3-5-sonnet-20241022"、"claude-3-opus-20240229"）。
    /// </summary>
    private readonly string _modelName;

    /// <summary>
    /// Default generation options applied to all requests (can be overridden per-request).
    /// 应用于所有请求的默认生成选项（可在每次请求时覆盖）。
    /// </summary>
    private readonly GenerateOptions? _defaultOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnthropicModel"/> class.
    /// 初始化 <see cref="AnthropicModel"/> 类的新实例。
    /// </summary>
    /// <param name="modelName">Model name (e.g., "claude-3-5-sonnet-20241022") / 模型名称。</param>
    /// <param name="apiKey">API key (optional, falls back to ANTHROPIC_API_KEY env var) / API 密钥（可选，未提供则读取环境变量 ANTHROPIC_API_KEY）。</param>
    /// <param name="baseUrl">Base URL (optional) / 基础地址（可选）。</param>
    /// <param name="formatter">Custom formatter (optional) / 自定义格式化器（可选）。</param>
    /// <param name="defaultOptions">Default generation options / 默认生成选项。</param>
    public AnthropicModel(
        string modelName,
        string? apiKey = null,
        string? baseUrl = null,
        AnthropicChatFormatter? formatter = null,
        GenerateOptions? defaultOptions = null)
        : base(modelName)
    {
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
        _apiKey = apiKey;
        _baseUrl = baseUrl;
        _formatter = formatter ?? new AnthropicChatFormatter(modelName);
        _defaultOptions = defaultOptions;

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AgentScope.NET/1.0");
    }

    /// <inheritdoc />
    public override IObservable<ModelResponse> Generate(ModelRequest request)
    {
        // Wrap the async method as an observable for Rx-style consumption
        // 将异步方法包装为可观察对象以支持 Rx 风格消费
        return Observable.FromAsync(async () => await GenerateAsync(request));
    }

    /// <inheritdoc />
    public override async Task<ModelResponse> GenerateAsync(ModelRequest request)
    {
        var messages = request.Messages;
        var options = MergeOptions(ConvertOptions(request.Options), _defaultOptions);

        // Step 1: Format AgentScope messages into Anthropic API request format
        // 步骤 1：将 AgentScope 消息格式化为 Anthropic API 请求格式
        var anthropicRequest = _formatter.Format(messages, options);

        // Step 2: Serialize request body to JSON with snake_case naming
        // 步骤 2：使用 snake_case 命名将请求体序列化为 JSON
        var json = JsonSerializer.Serialize(anthropicRequest, AnthropicSerializerOptions.Default);
        var url = BuildUrl(_baseUrl, MessagesEndpoint);

        // Step 3: Build HTTP request with Anthropic-specific headers
        // 步骤 3：使用 Anthropic 特定标头构建 HTTP 请求
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.TryAddWithoutValidation("x-api-key", GetApiKey(_apiKey));
        httpRequest.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // Step 4: Send request and read response
        // 步骤 4：发送请求并读取响应
        var response = await _httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new ModelException($"Anthropic API error: {response.StatusCode} - {responseBody} / Anthropic API 错误：{response.StatusCode} - {responseBody}");
        }

        // Step 5: Parse the response body into ParsedResponse
        // 步骤 5：将响应体解析为 ParsedResponse
        var parsedResponse = _formatter.Parse(responseBody);
        if (parsedResponse == null)
        {
            throw new ModelException("Failed to parse Anthropic response / 解析 Anthropic 响应失败");
        }

        return new ModelResponse
        {
            Text = parsedResponse.TextContent,
            Metadata = parsedResponse.ToolCalls?.Count > 0
                ? new Dictionary<string, object> { ["toolCalls"] = parsedResponse.ToolCalls }
                : null,
            Success = true
        };
    }

    /// <summary>
    /// Generates a streaming response from the Anthropic model using Server-Sent Events (SSE).
    /// Each SSE "data:" line is parsed and yielded as a ChatResponse chunk.
    /// 使用服务器发送事件（SSE）从 Anthropic 模型生成流式响应。
    /// 每个 SSE "data:" 行都被解析并作为 ChatResponse 块生成。
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

        // Step 1: Format messages into Anthropic request format with streaming enabled
        // 步骤 1：将消息格式化为启用流式的 Anthropic 请求格式
        var anthropicRequest = _formatter.Format(messages, mergedOptions);

        // Step 2: Serialize request to JSON
        // 步骤 2：将请求序列化为 JSON
        var json = JsonSerializer.Serialize(anthropicRequest, AnthropicSerializerOptions.Default);
        var url = BuildUrl(_baseUrl, MessagesEndpoint);

        // Step 3: Build HTTP request with streaming response headers
        // 步骤 3：构建带有流式响应标头的 HTTP 请求
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.TryAddWithoutValidation("x-api-key", GetApiKey(_apiKey));
        httpRequest.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // Step 4: Send request with ResponseHeadersRead to enable streaming
        // 步骤 4：使用 ResponseHeadersRead 发送请求以启用流式传输
        var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        // Step 5: Read the SSE stream line by line
        // 步骤 5：逐行读取 SSE 流
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Parse SSE format: "data: {...}"
            // 解析 SSE 格式："data: {...}"
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data == "[DONE]") yield break;

                var parsedResponse = _formatter.Parse(data);
                if (parsedResponse != null)
                {
                    yield return ConvertToChatResponse(parsedResponse);
                }
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
    /// Supports Anthropic-specific options like TopK and ThinkingBudget.
    /// 合并提供的选项与默认选项，提供的选项优先。
    /// 支持 Anthropic 特定选项如 TopK 和 ThinkingBudget。
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
            TopK = options.TopK ?? defaults.TopK,
            Stop = options.Stop ?? defaults.Stop,
            ThinkingBudget = options.ThinkingBudget ?? defaults.ThinkingBudget,
            ResponseFormat = options.ResponseFormat ?? defaults.ResponseFormat
        };

        return merged;
    }

    /// <summary>
    /// Converts a Dictionary&lt;string, object&gt; options map to a strongly-typed GenerateOptions.
    /// Supports Anthropic-specific options like topK and thinkingBudget.
    /// 将 Dictionary&lt;string, object&gt; 选项字典转换为强类型的 GenerateOptions。
    /// 支持 Anthropic 特定选项如 topK 和 thinkingBudget。
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
        if (options.TryGetValue("topK", out var topK) && topK is int topKValue)
            result.TopK = topKValue;
        if (options.TryGetValue("stop", out var stop) && stop is List<string> stopValue)
            result.Stop = stopValue;
        if (options.TryGetValue("thinkingBudget", out var thinkingBudget) && thinkingBudget is int thinkingBudgetValue)
            result.ThinkingBudget = thinkingBudgetValue;

        return result;
    }

    /// <summary>
    /// Converts a ParsedResponse from the formatter into a ChatResponse for the AgentScope framework.
    /// Handles text content, tool calls, and usage statistics.
    /// 将格式化器的 ParsedResponse 转换为 AgentScope 框架的 ChatResponse。
    /// 处理文本内容、工具调用和使用统计。
    /// </summary>
    /// <param name="parsed">Parsed response from the formatter / 来自格式化器的解析响应。</param>
    /// <returns>A ChatResponse ready for consumption / 准备消费的 ChatResponse。</returns>
    private ChatResponse ConvertToChatResponse(ParsedResponse parsed)
    {
        var chatResponse = new ChatResponse
        {
            Id = parsed.Id,
            Model = parsed.Model,
            Content = parsed.TextContent,
            StopReason = parsed.StopReason,
            Success = true
        };

        // Map token usage statistics if available
        // 映射令牌使用统计（如果可用）
        if (parsed.Usage != null)
        {
            chatResponse.Usage = new ChatUsage
            {
                InputTokens = parsed.Usage.InputTokens,
                OutputTokens = parsed.Usage.OutputTokens,
                TotalTokens = parsed.Usage.InputTokens + parsed.Usage.OutputTokens
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
                    Name = tc.Name ?? string.Empty,
                    Type = "function",
                    Arguments = tc.InputJson ?? string.Empty
                });
            }
        }

        return chatResponse;
    }

    /// <summary>
    /// Builds the full URL by combining the base URL and endpoint path.
    /// 通过组合基础 URL 和端点路径构建完整 URL。
    /// </summary>
    /// <param name="baseUrl">Base URL (optional, defaults to DefaultBaseUrl) / 基础 URL（可选，默认为 DefaultBaseUrl）。</param>
    /// <param name="endpoint">API endpoint path / API 端点路径。</param>
    /// <returns>Full URL for the API request / API 请求的完整 URL。</returns>
    private static string BuildUrl(string? baseUrl, string endpoint)
    {
        var baseUri = string.IsNullOrEmpty(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
        return baseUri + endpoint;
    }

    /// <summary>
    /// Retrieves the API key from the provided parameter or falls back to the ANTHROPIC_API_KEY environment variable.
    /// 从提供的参数中获取 API 密钥，或回退到 ANTHROPIC_API_KEY 环境变量。
    /// </summary>
    /// <param name="apiKey">API key parameter (optional) / API 密钥参数（可选）。</param>
    /// <returns>The API key string / API 密钥字符串。</returns>
    /// <exception cref="ModelException">Thrown when no API key is found / 未找到 API 密钥时抛出。</exception>
    private static string GetApiKey(string? apiKey)
    {
        if (!string.IsNullOrEmpty(apiKey)) return apiKey;

        var envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrEmpty(envKey)) return envKey;

        throw new ModelException(
            "Anthropic API key not found. Please set ANTHROPIC_API_KEY environment variable or provide apiKey parameter. / 未找到 Anthropic API 密钥。请设置 ANTHROPIC_API_KEY 环境变量或提供 apiKey 参数。");
    }
}

/// <summary>
/// JSON serializer options for the Anthropic API.
/// Uses snake_case property naming and ignores null values to match Anthropic's API contract.
/// Anthropic API 的 JSON 序列化选项。
/// 使用 snake_case 属性命名并忽略空值以匹配 Anthropic 的 API 约定。
/// </summary>
public static class AnthropicSerializerOptions
{
    /// <summary>
    /// Default serializer options: snake_case property naming, ignore null values.
    /// 默认序列化选项：snake_case 属性命名，忽略空值。
    /// </summary>
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
