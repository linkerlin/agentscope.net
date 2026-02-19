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
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AgentScope.Core.Formatter;
using AgentScope.Core.Formatter.Gemini;
using AgentScope.Core.Formatter.Gemini.Dto;
using AgentScope.Core.Message;
using AgentScope.Core.Model;

namespace AgentScope.Core.Model.Gemini;

/// <summary>
/// Google Gemini model provider.
/// Google Gemini 模型提供者
/// 
/// Features:
/// - Support for Gemini Pro, Gemini Pro Vision, Gemini 1.5 Pro, Gemini 1.5 Flash
/// - Multimodal input (text, images)
/// - Function calling support
/// - Safety settings
/// 
/// Environment variables:
/// - GOOGLE_API_KEY: Google AI API key
/// - GEMINI_MODEL: Model name (default: gemini-pro)
/// 
/// API documentation: https://ai.google.dev/docs
/// </summary>
public class GeminiModel : ModelBase
{
    /// <summary>
    /// Default Gemini API base URL
    /// </summary>
    public const string DefaultBaseUrl = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>
    /// Default Gemini model
    /// </summary>
    public const string DefaultModel = "gemini-pro";

    /// <summary>
    /// Available Gemini models
    /// </summary>
    public static class Models
    {
        /// <summary>
        /// Gemini Pro - Best for text generation
        /// </summary>
        public const string GeminiPro = "gemini-pro";

        /// <summary>
        /// Gemini Pro Vision - For image understanding
        /// </summary>
        public const string GeminiProVision = "gemini-pro-vision";

        /// <summary>
        /// Gemini 1.5 Pro - Latest Pro model with longer context
        /// </summary>
        public const string Gemini15Pro = "gemini-1.5-pro";

        /// <summary>
        /// Gemini 1.5 Flash - Faster, more efficient model
        /// </summary>
        public const string Gemini15Flash = "gemini-1.5-flash";

        /// <summary>
        /// Gemini 2.0 Flash - Latest Flash model
        /// </summary>
        public const string Gemini20Flash = "gemini-2.0-flash-exp";

        /// <summary>
        /// Gemini 2.0 Pro - Latest Pro model
        /// </summary>
        public const string Gemini20Pro = "gemini-2.0-pro-exp";
    }

    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private readonly GeminiFormatter _formatter;
    private readonly GenerateOptions? _defaultOptions;

    /// <summary>
    /// Gets the API key (masked for security)
    /// </summary>
    public string ApiKey => _apiKey.Length > 8 
        ? $"{_apiKey[..4]}...{_apiKey[^4..]}" 
        : "****";

    /// <summary>
    /// Gets the base URL
    /// </summary>
    public string BaseUrl => _baseUrl;

    /// <summary>
    /// Creates a new Gemini model instance.
    /// </summary>
    /// <param name="modelName">Model name (default: gemini-pro)</param>
    /// <param name="apiKey">Google AI API key (required)</param>
    /// <param name="baseUrl">API base URL (optional, uses default)</param>
    /// <param name="defaultOptions">Default generation options</param>
    public GeminiModel(
        string modelName = DefaultModel,
        string? apiKey = null,
        string? baseUrl = null,
        GenerateOptions? defaultOptions = null)
        : base(modelName)
    {
        _apiKey = apiKey ?? GetApiKey();
        _baseUrl = baseUrl ?? DefaultBaseUrl;
        _defaultOptions = defaultOptions;
        _httpClient = new HttpClient();
        _formatter = new GeminiFormatter(defaultOptions);
    }

    /// <summary>
    /// Get API key from environment variable.
    /// </summary>
    private static string GetApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
                  ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "Google API key is required. Set GOOGLE_API_KEY or GEMINI_API_KEY environment variable.");
        }

        return apiKey;
    }

    /// <summary>
    /// Generate response using Gemini API.
    /// </summary>
    public override IObservable<ModelResponse> Generate(ModelRequest request)
    {
        return Observable.FromAsync(() => GenerateAsync(request));
    }

    /// <summary>
    /// Generate response asynchronously using Gemini API.
    /// </summary>
    public override async Task<ModelResponse> GenerateAsync(ModelRequest request)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Extract options from request
            var options = ExtractOptions(request.Options);

            // Extract tools if present
            var tools = ExtractTools(request.Options);

            // Create Gemini request using formatter
            var geminiRequest = _formatter.CreateRequest(
                request.Messages,
                options: options,
                tools: tools
            );

            // Build API URL
            var url = $"{_baseUrl}/models/{ModelName}:generateContent?key={_apiKey}";

            // Serialize request
            var jsonContent = JsonSerializer.Serialize(geminiRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Send request
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new ModelResponse
                {
                    Success = false,
                    Error = $"API error: {response.StatusCode} - {errorContent}"
                };
            }

            // Parse response
            var responseContent = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (geminiResponse == null)
            {
                return new ModelResponse
                {
                    Success = false,
                    Error = "Failed to parse response"
                };
            }

            return _formatter.ParseResponse(geminiResponse, startTime);
        }
        catch (System.Exception ex)
        {
            return new ModelResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Extract GenerateOptions from request options dictionary.
    /// </summary>
    private GenerateOptions? ExtractOptions(Dictionary<string, object>? options)
    {
        if (options == null) return _defaultOptions;

        var generateOptions = new GenerateOptions();

        if (options.TryGetValue("temperature", out var temp) && temp is double temperature)
        {
            generateOptions.Temperature = temperature;
        }

        if (options.TryGetValue("maxTokens", out var maxTokens) && maxTokens is int max)
        {
            generateOptions.MaxTokens = max;
        }

        if (options.TryGetValue("topP", out var topP) && topP is double topPValue)
        {
            generateOptions.TopP = topPValue;
        }

        if (options.TryGetValue("topK", out var topK) && topK is int topKValue)
        {
            generateOptions.TopK = topKValue;
        }

        if (options.TryGetValue("stop", out var stop) && stop is List<string> stopSequences)
        {
            generateOptions.Stop = stopSequences;
        }

        return generateOptions;
    }

    /// <summary>
    /// Extract tool schemas from request options dictionary.
    /// </summary>
    private List<ToolSchema>? ExtractTools(Dictionary<string, object>? options)
    {
        if (options == null) return null;

        if (options.TryGetValue("tools", out var tools) && tools is List<ToolSchema> toolSchemas)
        {
            return toolSchemas;
        }

        return null;
    }

    /// <summary>
    /// Create a builder for GeminiModel.
    /// </summary>
    public static GeminiModelBuilder Builder()
    {
        return new GeminiModelBuilder();
    }
}

/// <summary>
/// Builder for GeminiModel.
/// </summary>
public class GeminiModelBuilder
{
    private string _modelName = GeminiModel.DefaultModel;
    private string? _apiKey;
    private string? _baseUrl;
    private GenerateOptions? _defaultOptions;

    /// <summary>
    /// Set the model name.
    /// </summary>
    public GeminiModelBuilder ModelName(string modelName)
    {
        _modelName = modelName;
        return this;
    }

    /// <summary>
    /// Set the API key.
    /// </summary>
    public GeminiModelBuilder ApiKey(string apiKey)
    {
        _apiKey = apiKey;
        return this;
    }

    /// <summary>
    /// Use Gemini Pro model.
    /// </summary>
    public GeminiModelBuilder UseGeminiPro()
    {
        _modelName = GeminiModel.Models.GeminiPro;
        return this;
    }

    /// <summary>
    /// Use Gemini Pro Vision model.
    /// </summary>
    public GeminiModelBuilder UseGeminiProVision()
    {
        _modelName = GeminiModel.Models.GeminiProVision;
        return this;
    }

    /// <summary>
    /// Use Gemini 1.5 Pro model.
    /// </summary>
    public GeminiModelBuilder UseGemini15Pro()
    {
        _modelName = GeminiModel.Models.Gemini15Pro;
        return this;
    }

    /// <summary>
    /// Use Gemini 1.5 Flash model.
    /// </summary>
    public GeminiModelBuilder UseGemini15Flash()
    {
        _modelName = GeminiModel.Models.Gemini15Flash;
        return this;
    }

    /// <summary>
    /// Use Gemini 2.0 Flash model.
    /// </summary>
    public GeminiModelBuilder UseGemini20Flash()
    {
        _modelName = GeminiModel.Models.Gemini20Flash;
        return this;
    }

    /// <summary>
    /// Set the base URL for Gemini API.
    /// </summary>
    public GeminiModelBuilder BaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        return this;
    }

    /// <summary>
    /// Set default generation options.
    /// </summary>
    public GeminiModelBuilder DefaultOptions(GenerateOptions options)
    {
        _defaultOptions = options;
        return this;
    }

    /// <summary>
    /// Build the GeminiModel instance.
    /// </summary>
    public GeminiModel Build()
    {
        return new GeminiModel(_modelName, _apiKey, _baseUrl, _defaultOptions);
    }
}
