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
/// Anthropic Claude Model using native HTTP API.
/// Anthropic Claude 模型
/// 
/// Java参考: io.agentscope.core.model.AnthropicChatModel
/// </summary>
public class AnthropicModel : ModelBase
{
    public const string DefaultBaseUrl = "https://api.anthropic.com";
    public const string MessagesEndpoint = "/v1/messages";

    private readonly HttpClient _httpClient;
    private readonly AnthropicChatFormatter _formatter;
    private readonly string? _apiKey;
    private readonly string? _baseUrl;
    private readonly string _modelName;
    private readonly GenerateOptions? _defaultOptions;

/// <summary>
    /// 创建新的 Anthropic 模型实例。
    /// </summary>
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
        return Observable.FromAsync(async () => await GenerateAsync(request));
    }

    /// <inheritdoc />
    public override async Task<ModelResponse> GenerateAsync(ModelRequest request)
    {
        var messages = request.Messages;
        var options = MergeOptions(ConvertOptions(request.Options), _defaultOptions);

        // Format messages
        var anthropicRequest = _formatter.Format(messages, options);

        // Make API call
        var json = JsonSerializer.Serialize(anthropicRequest, AnthropicSerializerOptions.Default);
        var url = BuildUrl(_baseUrl, MessagesEndpoint);
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.TryAddWithoutValidation("x-api-key", GetApiKey(_apiKey));
        httpRequest.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new ModelException($"Anthropic API 错误：{response.StatusCode} - {responseBody}");
        }

        // Parse response
        var parsedResponse = _formatter.Parse(responseBody);
        if (parsedResponse == null)
        {
            throw new ModelException("解析 Anthropic 响应失败");
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
    /// 生成流式响应。
    /// </summary>
    public async IAsyncEnumerable<ChatResponse> GenerateStreamAsync(
        List<Msg> messages,
        GenerateOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var mergedOptions = MergeOptions(options, _defaultOptions);
        mergedOptions ??= new GenerateOptions();
        mergedOptions.Stream = true;

        // Format messages
        var anthropicRequest = _formatter.Format(messages, mergedOptions);

        // Serialize request
        var json = JsonSerializer.Serialize(anthropicRequest, AnthropicSerializerOptions.Default);
        var url = BuildUrl(_baseUrl, MessagesEndpoint);

        // Create HTTP request
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.TryAddWithoutValidation("x-api-key", GetApiKey(_apiKey));
        httpRequest.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // Send request
        var response = await _httpClient.SendAsync(
            httpRequest, 
            HttpCompletionOption.ResponseHeadersRead, 
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

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
    /// Merge provided options with default options.
    /// </summary>
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
    /// Convert Dictionary<string, object> to GenerateOptions.
    /// </summary>
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
    /// Convert ParsedResponse to ChatResponse.
    /// </summary>
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

        if (parsed.Usage != null)
        {
            chatResponse.Usage = new ChatUsage
            {
                InputTokens = parsed.Usage.InputTokens,
                OutputTokens = parsed.Usage.OutputTokens,
                TotalTokens = parsed.Usage.InputTokens + parsed.Usage.OutputTokens
            };
        }

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
    /// Build full URL from base URL and endpoint.
    /// </summary>
    private static string BuildUrl(string? baseUrl, string endpoint)
    {
        var baseUri = string.IsNullOrEmpty(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
        return baseUri + endpoint;
    }

    /// <summary>
    /// Get API key from parameter or environment variable.
    /// </summary>
    private static string GetApiKey(string? apiKey)
    {
        if (!string.IsNullOrEmpty(apiKey)) return apiKey;
        
        var envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrEmpty(envKey)) return envKey;
        
throw new ModelException(
            "未找到 Anthropic API 密钥。请设置 ANTHROPIC_API_KEY 环境变量或提供 apiKey 参数。");
    }
}

/// <summary>
/// Anthropic API 的序列化选项。
/// </summary>
public static class AnthropicSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
