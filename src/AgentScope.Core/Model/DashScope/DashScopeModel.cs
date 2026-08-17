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
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Formatter.DashScope;
using AgentScope.Core.Formatter.DashScope.Dto;
using AgentScope.Core.Message;

using GenerateOptions = AgentScope.Core.Formatter.DashScope.GenerateOptions;

namespace AgentScope.Core.Model.DashScope;

/// <summary>
/// DashScope (Aliyun Qwen) Model using native HTTP API for the AgentScope framework.
/// Supports both streaming and non-streaming chat completions, tool calling,
/// and Qwen-specific features like thinking/reasoning and web search.
/// Uses the OpenAI-compatible mode endpoint (/compatible-mode/v1/chat/completions)
/// with SSE-based streaming.
/// Corresponds to Java: io.agentscope.core.model.DashScopeChatModel
/// AgentScope 框架的通义千问（DashScope）模型，使用原生 HTTP API。
/// 支持流式和非流式聊天补全、工具调用，
/// 以及通义千问特有的思考/推理和联网搜索功能。
/// 使用 OpenAI 兼容模式端点 (/compatible-mode/v1/chat/completions)
/// 和基于 SSE 的流式传输。
/// 对应 Java: io.agentscope.core.model.DashScopeChatModel
/// </summary>
public class DashScopeModel : ModelBase, IStreamingChatModel
{
    /// <summary>
    /// Default base URL for the DashScope API.
    /// DashScope API 的默认基础地址。
    /// </summary>
    public const string DefaultBaseUrl = "https://dashscope.aliyuncs.com";

    /// <summary>
    /// Chat completion API endpoint path (OpenAI-compatible mode).
    /// DashScope provides an OpenAI-compatible endpoint for easier migration.
    /// 聊天补全 API 端点路径（OpenAI 兼容模式）。
    /// DashScope 提供 OpenAI 兼容端点以便于迁移。
    /// </summary>
    public const string ChatEndpoint = "/compatible-mode/v1/chat/completions";

    /// <summary>
    /// HTTP client for communicating with the DashScope API.
    /// 用于与 DashScope API 通信的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Formatter for converting AgentScope messages to DashScope request format and parsing responses.
    /// 用于将 AgentScope 消息转换为 DashScope 请求格式并解析响应的格式化器。
    /// </summary>
    private readonly DashScopeChatFormatter _formatter;

    /// <summary>
    /// API key for authentication (optional, falls back to DASHSCOPE_API_KEY env var).
    /// 用于身份验证的 API 密钥（可选，未提供则读取环境变量 DASHSCOPE_API_KEY）。
    /// </summary>
    private readonly string? _apiKey;

    /// <summary>
    /// Custom base URL for the API endpoint (optional, defaults to https://dashscope.aliyuncs.com).
    /// API 端点的自定义基础 URL（可选，默认为 https://dashscope.aliyuncs.com）。
    /// </summary>
    private readonly string? _baseUrl;

    /// <summary>
    /// The model identifier (e.g., "qwen-turbo", "qwen-plus", "qwen-max").
    /// 模型标识符（例如 "qwen-turbo"、"qwen-plus"、"qwen-max"）。
    /// </summary>
    private readonly string _modelName;

    /// <summary>
    /// Default generation options applied to all requests (can be overridden per-request).
    /// 应用于所有请求的默认生成选项（可在每次请求时覆盖）。
    /// </summary>
    private readonly GenerateOptions? _defaultOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashScopeModel"/> class.
    /// 初始化 <see cref="DashScopeModel"/> 类的新实例。
    /// </summary>
    /// <param name="modelName">Model name (e.g., "qwen-plus") / 模型名称。</param>
    /// <param name="apiKey">API key (optional, falls back to DASHSCOPE_API_KEY env var) / API 密钥（可选，未提供则读取环境变量 DASHSCOPE_API_KEY）。</param>
    /// <param name="baseUrl">Base URL (optional) / 基础地址（可选）。</param>
    /// <param name="formatter">Custom formatter (optional) / 自定义格式化器（可选）。</param>
    /// <param name="defaultOptions">Default generation options / 默认生成选项。</param>
    public DashScopeModel(
        string modelName,
        string? apiKey = null,
        string? baseUrl = null,
        DashScopeChatFormatter? formatter = null,
        GenerateOptions? defaultOptions = null)
        : base(modelName)
    {
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
        _apiKey = apiKey;
        _baseUrl = baseUrl;
        _formatter = formatter ?? new DashScopeChatFormatter(modelName);
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

        // Step 1: Convert AgentScope messages to DashScope message format
        // 步骤 1：将 AgentScope 消息转换为 DashScope 消息格式
        var dsMessages = Formatter.DashScope.DashScopeMessageConverter.Convert(messages);

        // Step 2: Build the DashScope API request with model, messages, and options
        // 步骤 2：构建包含模型、消息和选项的 DashScope API 请求
        var dashscopeRequest = BuildRequest(_modelName, dsMessages, false, options);

        // Step 3: Serialize request to JSON with snake_case naming
        // 步骤 3：使用 snake_case 命名将请求序列化为 JSON
        var json = JsonSerializer.Serialize(dashscopeRequest, DashScopeSerializerOptions.Default);
        var url = BuildUrl(_baseUrl, ChatEndpoint);

        // Step 4: Build HTTP request with Bearer token authentication
        // 步骤 4：构建带有 Bearer 令牌身份验证的 HTTP 请求
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {GetApiKey(_apiKey)}");
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // Step 5: Send request and read response
        // 步骤 5：发送请求并读取响应
        var response = await _httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new ModelException($"DashScope API error: {response.StatusCode} - {responseBody} / DashScope API 错误：{response.StatusCode} - {responseBody}");
        }

        // Step 6: Parse the response JSON into ParsedResponse
        // 步骤 6：将响应 JSON 解析为 ParsedResponse
        var parsedResponse = ParseResponse(responseBody);
        if (parsedResponse == null)
        {
            throw new ModelException("Failed to parse DashScope response / 解析 DashScope 响应失败");
        }

        return new ModelResponse
        {
            Text = parsedResponse.TextContent,
            Metadata = BuildMetadata(parsedResponse),
            Success = true
        };
    }

    /// <summary>
    /// Builds a metadata dictionary from the parsed response, including tool calls and thinking content.
    /// DashScope supports Qwen-specific features like reasoning/thinking content and web search results.
    /// 从解析的响应构建元数据字典，包括工具调用和思考内容。
    /// DashScope 支持通义千问特有的功能，如推理/思考内容和联网搜索结果。
    /// </summary>
    /// <param name="parsedResponse">Parsed response from the API / 来自 API 的解析响应。</param>
    /// <returns>Metadata dictionary or null if no metadata is present / 元数据字典，如果没有元数据则返回 null。</returns>
    private static Dictionary<string, object>? BuildMetadata(ParsedResponse parsedResponse)
    {
        if (parsedResponse.ToolCalls?.Count > 0 || !string.IsNullOrEmpty(parsedResponse.ThinkingContent))
        {
            var metadata = new Dictionary<string, object>();
            if (parsedResponse.ToolCalls?.Count > 0)
            {
                metadata["toolCalls"] = parsedResponse.ToolCalls;
            }
            if (!string.IsNullOrEmpty(parsedResponse.ThinkingContent))
            {
                metadata["thinking"] = parsedResponse.ThinkingContent;
            }
            return metadata;
        }
        return null;
    }

    /// <summary>
    /// Generates a streaming response from the DashScope model using Server-Sent Events (SSE).
    /// Each SSE "data:" line is parsed and yielded as a ChatResponse chunk.
    /// Supports incremental output for real-time text streaming.
    /// 使用服务器发送事件（SSE）从 DashScope 模型生成流式响应。
    /// 每个 SSE "data:" 行都被解析并作为 ChatResponse 块生成。
    /// 支持增量输出以实现实时文本流式传输。
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

        // Step 1: Convert messages and build streaming request
        // 步骤 1：转换消息并构建流式请求
        var dsMessages = Formatter.DashScope.DashScopeMessageConverter.Convert(messages);
        var dashscopeRequest = BuildRequest(_modelName, dsMessages, true, mergedOptions);

        // Step 2: Serialize request to JSON
        // 步骤 2：将请求序列化为 JSON
        var json = JsonSerializer.Serialize(dashscopeRequest, DashScopeSerializerOptions.Default);
        var url = BuildUrl(_baseUrl, ChatEndpoint);

        // Step 3: Build HTTP request with Bearer token
        // 步骤 3：构建带有 Bearer 令牌的 HTTP 请求
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {GetApiKey(_apiKey)}");
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

                var parsedResponse = ParseResponse(data);
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
    /// Builds a DashScope API request with the specified model, messages, streaming flag, and options.
    /// Supports Qwen-specific parameters like EnableThinking, ThinkingBudget, EnableSearch,
    /// and IncrementalOutput for streaming.
    /// 构建一个包含指定模型、消息、流标识和选项的 DashScope API 请求。
    /// 支持通义千问特有参数，如 EnableThinking、ThinkingBudget、EnableSearch
    /// 和 IncrementalOutput（用于流式传输）。
    /// </summary>
    /// <param name="model">Model name / 模型名称。</param>
    /// <param name="messages">List of DashScope-formatted messages / DashScope 格式的消息列表。</param>
    /// <param name="stream">Whether to enable streaming / 是否启用流式传输。</param>
    /// <param name="options">Generation options / 生成选项。</param>
    /// <returns>A DashScopeRequest ready to be serialized / 准备序列化的 DashScopeRequest。</returns>
    private DashScopeRequest BuildRequest(string model, List<DashScopeMessage> messages, bool stream, GenerateOptions? options)
    {
        var parameters = new DashScopeParameters
        {
            ResultFormat = "message",
            IncrementalOutput = stream
        };

        // Apply all configured options to parameters
        // 将所有已配置的选项应用到参数中
        if (options != null)
        {
            if (options.Temperature.HasValue)
                parameters.Temperature = options.Temperature.Value;
            if (options.MaxTokens.HasValue)
                parameters.MaxTokens = options.MaxTokens.Value;
            if (options.TopP.HasValue)
                parameters.TopP = options.TopP.Value;
            if (options.TopK.HasValue)
                parameters.TopK = options.TopK.Value;
            if (options.Seed.HasValue)
                parameters.Seed = options.Seed.Value;
            if (options.EnableThinking.HasValue)
                parameters.EnableThinking = options.EnableThinking.Value;
            if (options.ThinkingBudget.HasValue)
                parameters.ThinkingBudget = options.ThinkingBudget.Value;
            if (options.IncrementalOutput.HasValue)
                parameters.IncrementalOutput = options.IncrementalOutput.Value;
            if (options.EnableSearch.HasValue)
                parameters.EnableSearch = options.EnableSearch.Value;
            if (options.Stop?.Count > 0)
                parameters.Stop = options.Stop;

            // Apply tool definitions if present (function calling support)
            // 如果存在工具定义，则应用工具（函数调用支持）
            if (options.Tools?.Count > 0)
            {
                parameters.Tools = options.Tools.Select(t => new DashScopeTool
                {
                    Type = "function",
                    Function = new DashScopeToolFunction
                    {
                        Name = t.Name,
                        Description = t.Description,
                        Parameters = t.Parameters != null
                            ? new Dictionary<string, object>
                            {
                                ["type"] = t.Parameters.Type,
                                ["properties"] = t.Parameters.Properties?.ToDictionary(
                                    p => p.Key,
                                    p => (object)new { type = p.Value.Type, description = p.Value.Description }) ?? new Dictionary<string, object>(),
                                ["required"] = t.Parameters.Required ?? new List<string>()
                            }
                            : null
                    }
                }).ToList();
            }
        }

        return new DashScopeRequest
        {
            Model = model,
            Input = new DashScopeInput { Messages = messages },
            Parameters = parameters
        };
    }

    /// <summary>
    /// Parses a JSON response string from the DashScope API into a ParsedResponse.
    /// Handles both string and list content formats, tool calls, and thinking content.
    /// 将 DashScope API 的 JSON 响应字符串解析为 ParsedResponse。
    /// 处理字符串和列表两种内容格式、工具调用和思考内容。
    /// </summary>
    /// <param name="json">Raw JSON response string / 原始 JSON 响应字符串。</param>
    /// <returns>ParsedResponse or null if parsing fails / 解析后的 ParsedResponse，如果解析失败则返回 null。</returns>
    private ParsedResponse? ParseResponse(string json)
    {
        try
        {
            var response = JsonSerializer.Deserialize<DashScopeResponse>(json, DashScopeSerializerOptions.Default);
            if (response == null || response.Output?.Choices == null || response.Output.Choices.Count == 0)
                return null;

            var choice = response.Output.Choices[0];
            var message = choice.Message;
            if (message == null)
                return null;

            var result = new ParsedResponse
            {
                Id = response.RequestId,
                FinishReason = choice.FinishReason ?? response.Output.FinishReason,
                Usage = response.Usage != null ? new UsageInfo
                {
                    InputTokens = response.Usage.InputTokens ?? 0,
                    OutputTokens = response.Usage.OutputTokens ?? 0,
                    TotalTokens = response.Usage.TotalTokens ?? 0
                } : null,
                ThinkingContent = message.ReasoningContent
            };

            // Extract text content - handles both string and list formats
            // DashScope may return content as a string or a list of content blocks
            // 提取文本内容 - 处理字符串和列表两种格式
            // DashScope 可能以字符串或内容块列表的形式返回内容
            if (message.Content is string strContent)
            {
                result.TextContent = strContent;
            }
            else if (message.Content is List<object> listContent)
            {
                result.TextContent = string.Join("", listContent.Select(c => c?.ToString() ?? ""));
            }
            else
            {
                result.TextContent = message.Content?.ToString() ?? "";
            }

            // Extract tool calls if present (function calling)
            // 提取工具调用（如果存在）（函数调用）
            if (message.ToolCalls?.Count > 0)
            {
                result.ToolCalls = message.ToolCalls.Select(t => new ToolCall
                {
                    Id = t.Id,
                    Type = t.Type,
                    Function = new FunctionInfo
                    {
                        Name = t.Function.Name,
                        Arguments = t.Function.Arguments
                    }
                }).ToList();
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Merges provided options with default options, with provided options taking precedence.
    /// Supports DashScope-specific options like EnableThinking, EnableSearch, and IncrementalOutput.
    /// 合并提供的选项与默认选项，提供的选项优先。
    /// 支持 DashScope 特有选项如 EnableThinking、EnableSearch 和 IncrementalOutput。
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
            Seed = options.Seed ?? defaults.Seed,
            Stop = options.Stop ?? defaults.Stop,
            EnableThinking = options.EnableThinking ?? defaults.EnableThinking,
            ThinkingBudget = options.ThinkingBudget ?? defaults.ThinkingBudget,
            IncrementalOutput = options.IncrementalOutput ?? defaults.IncrementalOutput,
            EnableSearch = options.EnableSearch ?? defaults.EnableSearch,
            Tools = options.Tools ?? defaults.Tools
        };

        return merged;
    }

    /// <summary>
    /// Converts a Dictionary&lt;string, object&gt; options map to a strongly-typed GenerateOptions.
    /// Supports DashScope-specific options like enableThinking, enableSearch, and incrementalOutput.
    /// 将 Dictionary&lt;string, object&gt; 选项字典转换为强类型的 GenerateOptions。
    /// 支持 DashScope 特有选项如 enableThinking、enableSearch 和 incrementalOutput。
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
        if (options.TryGetValue("seed", out var seed) && seed is int seedValue)
            result.Seed = seedValue;
        if (options.TryGetValue("stop", out var stop) && stop is List<string> stopValue)
            result.Stop = stopValue;
        if (options.TryGetValue("enableThinking", out var enableThinking) && enableThinking is bool enableThinkingValue)
            result.EnableThinking = enableThinkingValue;
        if (options.TryGetValue("thinkingBudget", out var thinkingBudget) && thinkingBudget is int thinkingBudgetValue)
            result.ThinkingBudget = thinkingBudgetValue;
        if (options.TryGetValue("incrementalOutput", out var incrementalOutput) && incrementalOutput is bool incrementalOutputValue)
            result.IncrementalOutput = incrementalOutputValue;
        if (options.TryGetValue("enableSearch", out var enableSearch) && enableSearch is bool enableSearchValue)
            result.EnableSearch = enableSearchValue;

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
            Content = parsed.TextContent,
            StopReason = parsed.FinishReason,
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
                    Name = tc.Function?.Name ?? string.Empty,
                    Type = tc.Type ?? "function",
                    Arguments = tc.Function?.Arguments ?? string.Empty
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
    /// Retrieves the API key from the provided parameter or falls back to the DASHSCOPE_API_KEY environment variable.
    /// 从提供的参数中获取 API 密钥，或回退到 DASHSCOPE_API_KEY 环境变量。
    /// </summary>
    /// <param name="apiKey">API key parameter (optional) / API 密钥参数（可选）。</param>
    /// <returns>The API key string / API 密钥字符串。</returns>
    /// <exception cref="ModelException">Thrown when no API key is found / 未找到 API 密钥时抛出。</exception>
    private static string GetApiKey(string? apiKey)
    {
        if (!string.IsNullOrEmpty(apiKey)) return apiKey;

        var envKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY");
        if (!string.IsNullOrEmpty(envKey)) return envKey;

        throw new ModelException(
            "DashScope API key not found. Please set DASHSCOPE_API_KEY environment variable or provide apiKey parameter.");
    }
}

/// <summary>
/// JSON serializer options for DashScope API (snake_case naming, ignore null values).
/// DashScope API 的 JSON 序列化选项（snake_case 命名，忽略空值）。
/// </summary>
public static class DashScopeSerializerOptions
{
    /// <summary>
    /// Default serializer options: snake_case property naming, ignore null values.
    /// 默认序列化选项：snake_case 属性命名，忽略空值。
    /// </summary>
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
