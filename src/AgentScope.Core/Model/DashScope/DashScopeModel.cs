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
/// DashScope (Aliyun Qwen) Model using native HTTP API.
/// 通义千问模型
/// 
/// Java参考: io.agentscope.core.model.DashScopeChatModel
/// </summary>
public class DashScopeModel : ModelBase
{
    public const string DefaultBaseUrl = "https://dashscope.aliyuncs.com";
    public const string ChatEndpoint = "/compatible-mode/v1/chat/completions";

    private readonly HttpClient _httpClient;
    private readonly DashScopeChatFormatter _formatter;
    private readonly string? _apiKey;
    private readonly string? _baseUrl;
    private readonly string _modelName;
    private readonly GenerateOptions? _defaultOptions;

    /// <summary>
    /// Creates a new DashScope model instance.
    /// </summary>
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
        return Observable.FromAsync(async () => await GenerateAsync(request));
    }

    /// <inheritdoc />
    public override async Task<ModelResponse> GenerateAsync(ModelRequest request)
    {
        var messages = request.Messages;
        var options = MergeOptions(ConvertOptions(request.Options), _defaultOptions);

        // Build request
        var dsMessages = Formatter.DashScope.DashScopeMessageConverter.Convert(messages);
        var dashscopeRequest = BuildRequest(_modelName, dsMessages, false, options);

        // Serialize request
        var json = JsonSerializer.Serialize(dashscopeRequest, DashScopeSerializerOptions.Default);
        var url = BuildUrl(_baseUrl, ChatEndpoint);

        // Create HTTP request
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {GetApiKey(_apiKey)}");
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        // Send request
        var response = await _httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new ModelException($"DashScope API error: {response.StatusCode} - {responseBody}");
        }

        // Parse response
        var parsedResponse = ParseResponse(responseBody);
        if (parsedResponse == null)
        {
            throw new ModelException("Failed to parse DashScope response");
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
    /// Generate streaming response.
    /// </summary>
    public async IAsyncEnumerable<ChatResponse> GenerateStreamAsync(
        List<Msg> messages,
        GenerateOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var mergedOptions = MergeOptions(options, _defaultOptions);
        mergedOptions ??= new GenerateOptions();

        // Build request
        var dsMessages = Formatter.DashScope.DashScopeMessageConverter.Convert(messages);
        var dashscopeRequest = BuildRequest(_modelName, dsMessages, true, mergedOptions);

        // Serialize request
        var json = JsonSerializer.Serialize(dashscopeRequest, DashScopeSerializerOptions.Default);
        var url = BuildUrl(_baseUrl, ChatEndpoint);

        // Create HTTP request
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {GetApiKey(_apiKey)}");
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

            // Parse SSE format: "data: {...}"
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
    /// Build DashScope request.
    /// </summary>
    private DashScopeRequest BuildRequest(string model, List<DashScopeMessage> messages, bool stream, GenerateOptions? options)
    {
        var parameters = new DashScopeParameters
        {
            ResultFormat = "message",
            IncrementalOutput = stream
        };

        // Apply options
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

            // Apply tools
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
    /// Parse JSON response string to ParsedResponse.
    /// </summary>
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
                } : null
            };

            // Extract text content
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

            // Extract tool calls
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
    /// Convert ParsedResponse to ChatResponse.
    /// </summary>
    private ChatResponse ConvertToChatResponse(ParsedResponse parsed)
    {
        var chatResponse = new ChatResponse
        {
            Id = parsed.Id,
            Content = parsed.TextContent,
            StopReason = parsed.FinishReason,
            Success = true
        };

        if (parsed.Usage != null)
        {
            chatResponse.Usage = new ChatUsage
            {
                InputTokens = parsed.Usage.InputTokens,
                OutputTokens = parsed.Usage.OutputTokens,
                TotalTokens = parsed.Usage.TotalTokens
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
                    Name = tc.Function?.Name ?? string.Empty,
                    Type = tc.Type ?? "function",
                    Arguments = tc.Function?.Arguments ?? string.Empty
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
        
        var envKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY");
        if (!string.IsNullOrEmpty(envKey)) return envKey;
        
        throw new ModelException(
            "DashScope API key not found. Please set DASHSCOPE_API_KEY environment variable or provide apiKey parameter.");
    }
}

/// <summary>
/// Serializer options for DashScope API.
/// </summary>
public static class DashScopeSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
