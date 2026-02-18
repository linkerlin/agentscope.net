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
/// OpenAI Chat Model using native HTTP API.
/// OpenAI 聊天模型
/// 
/// Features:
/// - Streaming and non-streaming modes
/// - Tool calling support
/// - Automatic message format conversion
/// - Timeout and retry configuration
/// - Multi-provider support via different Formatters
/// 
/// Java参考: io.agentscope.core.model.OpenAIChatModel
/// </summary>
public class OpenAIModel : ModelBase
{
    private readonly OpenAIClient _client;
    private readonly OpenAIChatFormatter _formatter;
    private readonly string? _apiKey;
    private readonly string? _baseUrl;
    private readonly string _modelName;
    private readonly GenerateOptions? _defaultOptions;

    /// <summary>
    /// Creates a new OpenAI chat model instance.
    /// </summary>
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

    public override IObservable<模型响应> 生成 (模型请求 request)
    {
        return Observable.FromAsync(async () =>
        {
            var response = await GenerateAsync(request);
            return response;
        });
    }

    /// <inheritdoc />
    public override async Task<模型响应> GenerateAsync(模型请求 request)
    {
        var messages = request.Messages;
        var options = MergeOptions(ConvertOptions(request.Options), _defaultOptions);
        var startTime = DateTime.UtcNow;

        // Format messages
        var openaiRequest = _formatter.Format(messages, options);

        // Make API call
        var response = await _client.CallAsync(_apiKey, _baseUrl, openaiRequest);

        // Parse response
        var parsedResponse = _formatter.Parse(response);
        var chatResponse = ConvertToChatResponse(parsedResponse);
        return chatResponse;
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
        mergedOptions.Stream = true;

        // Format messages
        var openaiRequest = _formatter.Format(messages, mergedOptions);

        // Stream API call
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

            FrequencyPenalty = options.FrequencyPenalty ?? defaults.FrequencyPenalty,
            PresencePenalty = options.PresencePenalty ?? defaults.PresencePenalty,
            Seed = options.Seed ?? defaults.Seed,
            ResponseFormat = options.ResponseFormat ?? defaults.ResponseFormat,
            Stop = options.Stop ?? defaults.Stop
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
    /// Convert ParsedResponse to ChatResponse.
    /// </summary>
    private ChatResponse ConvertToChatResponse(ParsedResponse parsed)
    {
        var chatResponse = new ChatResponse
        {
            Id = parsed.Id,
            Model = parsed.Model,
            Content = parsed.TextContent,
            StopReason = parsed.FinishReason,
            Success = true
        };

        if (parsed.Usage != null)
        {
            chatResponse.Usage = new ChatUsage
            {
                InputTokens = parsed.Usage.PromptTokens,
                OutputTokens = parsed.Usage.CompletionTokens,
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
                    Name = tc.FunctionName ?? string.Empty,
                    Type = tc.Type,
                    Arguments = tc.FunctionArguments
                });
            }
        }

        return chatResponse;
    }

    /// <summary>
    /// Create a new builder for OpenAIModel.
    /// </summary>
    public static Builder CreateBuilder() => new();

    /// <summary>
    /// Builder for OpenAIModel.
    /// </summary>
    public class Builder
    {
        private string? _apiKey;
        private string? _modelName;
        private string? _baseUrl;
        private OpenAIClient? _client;
        private OpenAIChatFormatter? _formatter;
        private GenerateOptions? _defaultOptions;

        public Builder ApiKey(string apiKey)
        {
            _apiKey = apiKey;
            return this;
        }

        public Builder ModelName(string modelName)
        {
            _modelName = modelName;
            return this;
        }

        public Builder BaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl;
            return this;
        }

        public Builder Client(OpenAIClient client)
        {
            _client = client;
            return this;
        }

        public Builder Formatter(OpenAIChatFormatter formatter)
        {
            _formatter = formatter;
            return this;
        }

        public Builder DefaultOptions(GenerateOptions options)
        {
            _defaultOptions = options;
            return this;
        }

        public OpenAIModel Build()
        {
            if (string.IsNullOrEmpty(_modelName))
            {
                throw new ArgumentException("Model name must be set");
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
