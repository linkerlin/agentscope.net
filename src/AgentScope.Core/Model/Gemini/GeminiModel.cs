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
/// Google Gemini model provider for the AgentScope framework.
/// Supports Gemini Pro, Gemini Pro Vision, Gemini 1.5 Pro/Flash, and Gemini 2.0 models.
/// Provides multimodal input (text, images), function calling, and safety settings.
/// Uses the Google Generative Language API (v1beta) with API key authentication.
/// Corresponds to Java: io.agentscope.core.model.GeminiChatModel
/// AgentScope 框架的 Google Gemini 模型提供者。
/// 支持 Gemini Pro、Gemini Pro Vision、Gemini 1.5 Pro/Flash 和 Gemini 2.0 模型。
/// 提供多模态输入（文本、图像）、函数调用和安全设置。
/// 使用 Google Generative Language API (v1beta) 和 API 密钥身份验证。
/// 对应 Java: io.agentscope.core.model.GeminiChatModel
///
/// Features / 功能特性:
/// - Support for Gemini Pro, Gemini Pro Vision, Gemini 1.5 Pro, Gemini 1.5 Flash
///   支持 Gemini Pro、Gemini Pro Vision、Gemini 1.5 Pro、Gemini 1.5 Flash
/// - Multimodal input (text, images) / 多模态输入（文本、图像）
/// - Function calling support / 函数调用支持
/// - Safety settings / 安全设置
///
/// Environment variables / 环境变量:
/// - GOOGLE_API_KEY: Google AI API key / Google AI API 密钥
/// - GEMINI_API_KEY: Alternative environment variable for API key / API 密钥的备选环境变量
/// - GEMINI_MODEL: Model name (default: gemini-pro) / 模型名称（默认：gemini-pro）
///
/// API documentation: https://ai.google.dev/docs
/// </summary>
public class GeminiModel : ModelBase
{
    /// <summary>
    /// Default Gemini API base URL (v1beta).
    /// 默认 Gemini API 基础 URL (v1beta)。
    /// </summary>
    public const string DefaultBaseUrl = "https://generativelanguage.googleapis.com/v1beta";

    /// <summary>
    /// Default Gemini model name.
    /// 默认 Gemini 模型名称。
    /// </summary>
    public const string DefaultModel = "gemini-pro";

    /// <summary>
    /// Predefined Gemini model identifiers.
    /// 预定义的 Gemini 模型标识符。
    /// </summary>
    public static class Models
    {
        /// <summary>
        /// Gemini Pro - Best for text generation.
        /// Gemini Pro - 最适合文本生成。
        /// </summary>
        public const string GeminiPro = "gemini-pro";

        /// <summary>
        /// Gemini Pro Vision - For image understanding.
        /// Gemini Pro Vision - 用于图像理解。
        /// </summary>
        public const string GeminiProVision = "gemini-pro-vision";

        /// <summary>
        /// Gemini 1.5 Pro - Latest Pro model with longer context (up to 1M tokens).
        /// Gemini 1.5 Pro - 最新的 Pro 模型，支持更长的上下文（最多 100 万令牌）。
        /// </summary>
        public const string Gemini15Pro = "gemini-1.5-pro";

        /// <summary>
        /// Gemini 1.5 Flash - Faster, more efficient model.
        /// Gemini 1.5 Flash - 更快、更高效的模型。
        /// </summary>
        public const string Gemini15Flash = "gemini-1.5-flash";

        /// <summary>
        /// Gemini 2.0 Flash - Latest Flash model (experimental).
        /// Gemini 2.0 Flash - 最新的 Flash 模型（实验性）。
        /// </summary>
        public const string Gemini20Flash = "gemini-2.0-flash-exp";

        /// <summary>
        /// Gemini 2.0 Pro - Latest Pro model (experimental).
        /// Gemini 2.0 Pro - 最新的 Pro 模型（实验性）。
        /// </summary>
        public const string Gemini20Pro = "gemini-2.0-pro-exp";
    }

    /// <summary>
    /// API key for Google Generative Language API authentication.
    /// 用于 Google Generative Language API 身份验证的 API 密钥。
    /// </summary>
    private readonly string _apiKey;

    /// <summary>
    /// Base URL for the Gemini API.
    /// Gemini API 的基础 URL。
    /// </summary>
    private readonly string _baseUrl;

    /// <summary>
    /// HTTP client for communicating with the Gemini API.
    /// 用于与 Gemini API 通信的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Formatter for converting AgentScope messages to Gemini request format and parsing responses.
    /// 用于将 AgentScope 消息转换为 Gemini 请求格式并解析响应的格式化器。
    /// </summary>
    private readonly GeminiFormatter _formatter;

    /// <summary>
    /// Default generation options applied to all requests (can be overridden per-request).
    /// 应用于所有请求的默认生成选项（可在每次请求时覆盖）。
    /// </summary>
    private readonly GenerateOptions? _defaultOptions;

    /// <summary>
    /// Gets the API key with partial masking for security display purposes.
    /// 获取部分掩码的 API 密钥，用于安全显示。
    /// </summary>
    public string ApiKey => _apiKey.Length > 8 
        ? $"{_apiKey[..4]}...{_apiKey[^4..]}" 
        : "****";

    /// <summary>
    /// Gets the base URL for the Gemini API.
    /// 获取 Gemini API 的基础 URL。
    /// </summary>
    public string BaseUrl => _baseUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeminiModel"/> class.
    /// 初始化 <see cref="GeminiModel"/> 类的新实例。
    /// </summary>
    /// <param name="modelName">Model name (default: gemini-pro) / 模型名称（默认：gemini-pro）。</param>
    /// <param name="apiKey">Google AI API key (optional, falls back to GOOGLE_API_KEY or GEMINI_API_KEY env var) / Google AI API 密钥（可选，未提供则读取 GOOGLE_API_KEY 或 GEMINI_API_KEY 环境变量）。</param>
    /// <param name="baseUrl">API base URL (optional, uses default) / API 基础 URL（可选，使用默认值）。</param>
    /// <param name="defaultOptions">Default generation options / 默认生成选项。</param>
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
    /// Retrieves the API key from environment variables (GOOGLE_API_KEY or GEMINI_API_KEY).
    /// 从环境变量（GOOGLE_API_KEY 或 GEMINI_API_KEY）获取 API 密钥。
    /// </summary>
    /// <returns>The API key string / API 密钥字符串。</returns>
    /// <exception cref="InvalidOperationException">Thrown when no API key is found / 未找到 API 密钥时抛出。</exception>
    private static string GetApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
                  ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "Google API key is required. Set GOOGLE_API_KEY or GEMINI_API_KEY environment variable. / 需要 Google API 密钥。请设置 GOOGLE_API_KEY 或 GEMINI_API_KEY 环境变量。");
        }

        return apiKey;
    }

    /// <summary>
    /// Generates a response from the Gemini model using the Rx-style observable pattern.
    /// 使用 Rx 风格的可观察模式从 Gemini 模型生成响应。
    /// </summary>
    public override IObservable<ModelResponse> Generate(ModelRequest request)
    {
        return Observable.FromAsync(() => GenerateAsync(request));
    }

    /// <summary>
    /// Generates a response asynchronously from the Gemini model.
    /// Uses the generateContent endpoint with API key passed as a query parameter.
    /// 从 Gemini 模型异步生成响应。
    /// 使用 generateContent 端点，API 密钥作为查询参数传递。
    /// </summary>
    public override async Task<ModelResponse> GenerateAsync(ModelRequest request)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Step 1: Extract generation options from request options dictionary
            // 步骤 1：从请求选项字典中提取生成选项
            var options = ExtractOptions(request.Options);

            // Step 2: Extract tool schemas if present (function calling)
            // 步骤 2：提取工具模式（如果存在）（函数调用）
            var tools = ExtractTools(request.Options);

            // Step 3: Create Gemini request using the formatter
            // 步骤 3：使用格式化器创建 Gemini 请求
            var geminiRequest = _formatter.CreateRequest(
                request.Messages,
                options: options,
                tools: tools
            );

            // Step 4: Build API URL with API key as query parameter
            // 步骤 4：构建带有 API 密钥作为查询参数的 API URL
            var url = $"{_baseUrl}/models/{ModelName}:generateContent?key={_apiKey}";

            // Step 5: Serialize request to JSON with camelCase naming
            // 步骤 5：使用 camelCase 命名将请求序列化为 JSON
            var jsonContent = JsonSerializer.Serialize(geminiRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Step 6: Send request to Gemini API
            // 步骤 6：向 Gemini API 发送请求
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new ModelResponse
                {
                    Success = false,
                    Error = $"API error: {response.StatusCode} - {errorContent} / API 错误：{response.StatusCode} - {errorContent}"
                };
            }

            // Step 7: Parse the response
            // 步骤 7：解析响应
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
                    Error = "Failed to parse Gemini response / 解析 Gemini 响应失败"
                };
            }

            // Step 8: Use the formatter to convert Gemini response to ModelResponse
            // 步骤 8：使用格式化器将 Gemini 响应转换为 ModelResponse
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
    /// Extracts GenerateOptions from the request options dictionary.
    /// Supports standard options: temperature, maxTokens, topP, topK, stop.
    /// 从请求选项字典中提取 GenerateOptions。
    /// 支持标准选项：temperature、maxTokens、topP、topK、stop。
    /// </summary>
    /// <param name="options">Raw options dictionary from the request / 来自请求的原始选项字典。</param>
    /// <returns>Extracted GenerateOptions or default options if no request options / 提取的 GenerateOptions，如果没有请求选项则返回默认选项。</returns>
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
    /// Extracts tool schemas from the request options dictionary for function calling.
    /// 从请求选项字典中提取工具模式，用于函数调用。
    /// </summary>
    /// <param name="options">Raw options dictionary from the request / 来自请求的原始选项字典。</param>
    /// <returns>List of ToolSchema or null if no tools are configured / ToolSchema 列表，如果没有配置工具则返回 null。</returns>
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
    /// Creates a new builder for GeminiModel with fluent configuration.
    /// 创建一个新的 GeminiModel 构建器，支持流畅配置。
    /// </summary>
    /// <returns>A new GeminiModelBuilder instance / 一个新的 GeminiModelBuilder 实例。</returns>
    public static GeminiModelBuilder Builder()
    {
        return new GeminiModelBuilder();
    }
}

/// <summary>
/// Fluent builder for creating GeminiModel instances.
/// Provides convenient methods for selecting predefined models and configuring the API key.
/// 用于创建 GeminiModel 实例的流畅构建器。
/// 提供选择预定义模型和配置 API 密钥的便捷方法。
/// </summary>
public class GeminiModelBuilder
{
    /// <summary>
    /// The model name to use.
    /// 要使用的模型名称。
    /// </summary>
    private string _modelName = GeminiModel.DefaultModel;

    /// <summary>
    /// The API key for Google Generative Language API authentication.
    /// 用于 Google Generative Language API 身份验证的 API 密钥。
    /// </summary>
    private string? _apiKey;

    /// <summary>
    /// Custom base URL for the Gemini API.
    /// Gemini API 的自定义基础 URL。
    /// </summary>
    private string? _baseUrl;

    /// <summary>
    /// Default generation options.
    /// 默认生成选项。
    /// </summary>
    private GenerateOptions? _defaultOptions;

    /// <summary>
    /// Sets the model name.
    /// 设置模型名称。
    /// </summary>
    /// <param name="modelName">Model name / 模型名称。</param>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public GeminiModelBuilder ModelName(string modelName)
    {
        _modelName = modelName;
        return this;
    }

    /// <summary>
    /// Sets the API key for authentication.
    /// 设置用于身份验证的 API 密钥。
    /// </summary>
    /// <param name="apiKey">Google AI API key / Google AI API 密钥。</param>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public GeminiModelBuilder ApiKey(string apiKey)
    {
        _apiKey = apiKey;
        return this;
    }

    /// <summary>
    /// Uses the Gemini Pro model (best for text generation).
    /// 使用 Gemini Pro 模型（最适合文本生成）。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public GeminiModelBuilder UseGeminiPro()
    {
        _modelName = GeminiModel.Models.GeminiPro;
        return this;
    }

    /// <summary>
    /// Uses the Gemini Pro Vision model (for image understanding).
    /// 使用 Gemini Pro Vision 模型（用于图像理解）。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public GeminiModelBuilder UseGeminiProVision()
    {
        _modelName = GeminiModel.Models.GeminiProVision;
        return this;
    }

    /// <summary>
    /// Uses the Gemini 1.5 Pro model (latest Pro with longer context).
    /// 使用 Gemini 1.5 Pro 模型（最新的 Pro 模型，支持更长的上下文）。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public GeminiModelBuilder UseGemini15Pro()
    {
        _modelName = GeminiModel.Models.Gemini15Pro;
        return this;
    }

    /// <summary>
    /// Uses the Gemini 1.5 Flash model (faster, more efficient).
    /// 使用 Gemini 1.5 Flash 模型（更快、更高效）。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public GeminiModelBuilder UseGemini15Flash()
    {
        _modelName = GeminiModel.Models.Gemini15Flash;
        return this;
    }

    /// <summary>
    /// Uses the Gemini 2.0 Flash model (latest Flash, experimental).
    /// 使用 Gemini 2.0 Flash 模型（最新的 Flash 模型，实验性）。
    /// </summary>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public GeminiModelBuilder UseGemini20Flash()
    {
        _modelName = GeminiModel.Models.Gemini20Flash;
        return this;
    }

    /// <summary>
    /// Sets the base URL for the Gemini API.
    /// 设置 Gemini API 的基础 URL。
    /// </summary>
    /// <param name="baseUrl">Base URL / 基础 URL。</param>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public GeminiModelBuilder BaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl;
        return this;
    }

    /// <summary>
    /// Sets default generation options.
    /// 设置默认生成选项。
    /// </summary>
    /// <param name="options">Default generation options / 默认生成选项。</param>
    /// <returns>This builder instance for chaining / 此构建器实例，支持链式调用。</returns>
    public GeminiModelBuilder DefaultOptions(GenerateOptions options)
    {
        _defaultOptions = options;
        return this;
    }

    /// <summary>
    /// Builds the GeminiModel instance with the configured settings.
    /// 使用已配置的设置构建 GeminiModel 实例。
    /// </summary>
    /// <returns>A configured GeminiModel instance / 一个已配置的 GeminiModel 实例。</returns>
    public GeminiModel Build()
    {
        return new GeminiModel(_modelName, _apiKey, _baseUrl, _defaultOptions);
    }
}
