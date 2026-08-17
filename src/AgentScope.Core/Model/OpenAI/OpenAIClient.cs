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
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Formatter.OpenAI.Dto;
using AgentScope.Core.Model.Transport;

namespace AgentScope.Core.Model.OpenAI;

/// <summary>
/// Stateless HTTP client for OpenAI-compatible Chat Completion APIs.
/// Handles both synchronous (POST) and streaming (SSE) requests to the /v1/chat/completions endpoint.
/// All configuration (API key, base URL) is passed per-request, making this client stateless and reusable.
/// Corresponds to Java: io.agentscope.core.model.OpenAIClient
/// OpenAI 兼容聊天 API 的无状态 HTTP 客户端。
/// 处理对 /v1/chat/completions 端点的同步（POST）和流式（SSE）请求。
/// 所有配置（API 密钥、基础 URL）都在每次请求时传递，使此客户端无状态且可重用。
/// 对应 Java: io.agentscope.core.model.OpenAIClient
/// </summary>
public class OpenAIClient
{
    /// <summary>
    /// Default base URL for the OpenAI API.
    /// OpenAI API 的默认基础 URL。
    /// </summary>
    public const string DefaultBaseUrl = "https://api.openai.com";

    /// <summary>
    /// Chat completions API endpoint path.
    /// 聊天补全 API 端点路径。
    /// </summary>
    public const string ChatCompletionsEndpoint = "/v1/chat/completions";

    /// <summary>
    /// HTTP transport layer for executing requests (supports both standard HTTP and WebSocket).
    /// 用于执行请求的 HTTP 传输层（支持标准 HTTP 和 WebSocket）。
    /// </summary>
    private readonly IHttpTransport _transport;

    /// <summary>
    /// JSON serializer options configured for OpenAI API compatibility (snake_case naming, null omission).
    /// 为 OpenAI API 兼容性配置的 JSON 序列化选项（snake_case 命名、忽略 null）。
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIClient"/> class with default HTTP transport.
    /// 使用默认 HTTP 传输初始化 <see cref="OpenAIClient"/> 类的新实例。
    /// </summary>
    public OpenAIClient()
    {
        _transport = new HttpClientTransport();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIClient"/> class with a custom transport.
    /// 使用自定义传输初始化 <see cref="OpenAIClient"/> 类的新实例。
    /// </summary>
    /// <param name="transport">Custom HTTP transport implementation / 自定义 HTTP 传输实现。</param>
    public OpenAIClient(IHttpTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Makes a synchronous (non-streaming) call to the OpenAI Chat Completion API.
    /// Serializes the request, sends it via HTTP POST, and deserializes the response.
    /// 向 OpenAI 聊天补全 API 发起同步（非流式）调用。
    /// 序列化请求，通过 HTTP POST 发送，并反序列化响应。
    /// </summary>
    /// <param name="apiKey">API key for authentication / API 密钥。</param>
    /// <param name="baseUrl">Custom base URL (optional) / 自定义基础 URL（可选）。</param>
    /// <param name="request">The OpenAI request payload / OpenAI 请求负载。</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌。</param>
    /// <returns>Deserialized OpenAI response / 反序列化的 OpenAI 响应。</returns>
    /// <exception cref="ModelException">Thrown on API error or deserialization failure / API 错误或反序列化失败时抛出。</exception>
    public async Task<OpenAIResponse> CallAsync(
        string? apiKey,
        string? baseUrl,
        OpenAIRequest request,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(baseUrl, ChatCompletionsEndpoint);
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Build the HTTP request with authentication and content type headers
        // 构建带有身份验证和内容类型标头的 HTTP 请求
        var httpRequest = new HttpRequestBuilder()
            .Url(url)
            .Method("POST")
            .Header("Authorization", $"Bearer {GetApiKey(apiKey)}")
            .Header("Content-Type", "application/json")
            .Body(json)
            .Build();

        var response = await _transport.ExecuteAsync(httpRequest, cancellationToken);

        // Check for HTTP-level errors
        // 检查 HTTP 级别错误
        if (!response.IsSuccessStatusCode)
        {
            throw new ModelException(
                $"OpenAI API error: {response.StatusCode} - {response.Body}");
        }

        // Deserialize the JSON response body
        // 反序列化 JSON 响应体
        var result = JsonSerializer.Deserialize<OpenAIResponse>(response.Body, _jsonOptions);
        if (result == null)
        {
            throw new ModelException("Failed to deserialize OpenAI response / 反序列化 OpenAI 响应失败");
        }

        return result;
    }

    /// <summary>
    /// Makes a streaming call to the OpenAI Chat Completion API using Server-Sent Events (SSE).
    /// Each SSE "data:" line is parsed and yielded as an OpenAIResponse chunk.
    /// 使用服务器发送事件（SSE）向 OpenAI 聊天补全 API 发起流式调用。
    /// 每个 SSE "data:" 行都被解析并作为 OpenAIResponse 块生成。
    /// </summary>
    /// <param name="apiKey">API key for authentication / API 密钥。</param>
    /// <param name="baseUrl">Custom base URL (optional) / 自定义基础 URL（可选）。</param>
    /// <param name="request">The OpenAI request payload / OpenAI 请求负载。</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌。</param>
    /// <returns>Async enumerable of response chunks / 响应块的异步可枚举序列。</returns>
    public IAsyncEnumerable<OpenAIResponse> StreamAsync(
        string? apiKey,
        string? baseUrl,
        OpenAIRequest request,
        CancellationToken cancellationToken = default)
    {
        // Cannot use yield in a method with try-catch, so delegate to a separate async enumerable method
        // 不能在包含 try-catch 的方法中使用 yield，因此委托给单独的异步可枚举方法
        return StreamAsyncInternal(apiKey, baseUrl, request, cancellationToken);
    }

    /// <summary>
    /// Internal implementation of the streaming call that processes SSE events.
    /// 流式调用的内部实现，处理 SSE 事件。
    /// </summary>
    private async IAsyncEnumerable<OpenAIResponse> StreamAsyncInternal(
        string? apiKey,
        string? baseUrl,
        OpenAIRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var url = BuildUrl(baseUrl, ChatCompletionsEndpoint);
        var json = JsonSerializer.Serialize(request, _jsonOptions);

        // Build the HTTP request with SSE-specific Accept header
        // 构建带有 SSE 特定 Accept 标头的 HTTP 请求
        var httpRequest = new HttpRequestBuilder()
            .Url(url)
            .Method("POST")
            .Header("Authorization", $"Bearer {GetApiKey(apiKey)}")
            .Header("Content-Type", "application/json")
            .Header("Accept", "text/event-stream")
            .Body(json)
            .Build();

        // Process each line from the SSE stream
        // 处理 SSE 流中的每一行
        await foreach (var line in _transport.StreamAsync(httpRequest, cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Parse SSE format: "data: {...}"
            // 解析 SSE 格式："data: {...}"
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);

                // Check for the stream end signal
                // 检查流结束信号
                if (data == "[DONE]")
                {
                    yield break;
                }

                OpenAIResponse? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<OpenAIResponse>(data, _jsonOptions);
                }
                catch (JsonException)
                {
                    // Skip malformed chunks that can't be deserialized
                    // 跳过无法反序列化的格式错误块
                    continue;
                }

                if (chunk != null)
                {
                    yield return chunk;
                }
            }
        }
    }

    /// <summary>
    /// Builds the full URL by combining the base URL and endpoint path.
    /// Handles duplicate "/v1" prefix when using OpenAI-compatible APIs (e.g., DashScope).
    /// 通过组合基础 URL 和端点路径构建完整 URL。
    /// 处理使用 OpenAI 兼容 API（如 DashScope）时的重复 "/v1" 前缀。
    /// </summary>
    /// <param name="baseUrl">Base URL (optional, defaults to DefaultBaseUrl) / 基础 URL（可选，默认为 DefaultBaseUrl）。</param>
    /// <param name="endpoint">API endpoint path / API 端点路径。</param>
    /// <returns>Full URL for the API request / API 请求的完整 URL。</returns>
    private static string BuildUrl(string? baseUrl, string endpoint)
    {
        var baseUri = string.IsNullOrEmpty(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
        
        // If baseUrl already ends with /v1, and endpoint starts with /v1, 
        // remove the duplicate /v1 to handle OpenAI-compatible APIs like DashScope
        // 如果 baseUrl 已以 /v1 结尾，且 endpoint 以 /v1 开头，
        // 则移除重复的 /v1 以处理 DashScope 等 OpenAI 兼容 API
        if (endpoint.StartsWith("/v1/") && baseUri.EndsWith("/v1"))
        {
            endpoint = endpoint.Substring(3); // Remove "/v1" prefix, keep the rest / 移除 "/v1" 前缀，保留其余部分
        }
        
        return baseUri + endpoint;
    }

    /// <summary>
    /// Retrieves the API key from the provided parameter or falls back to the OPENAI_API_KEY environment variable.
    /// 从提供的参数中获取 API 密钥，或回退到 OPENAI_API_KEY 环境变量。
    /// </summary>
    /// <param name="apiKey">API key parameter (optional) / API 密钥参数（可选）。</param>
    /// <returns>The API key string / API 密钥字符串。</returns>
    /// <exception cref="ModelException">Thrown when no API key is found / 未找到 API 密钥时抛出。</exception>
    private static string GetApiKey(string? apiKey)
    {
        if (!string.IsNullOrEmpty(apiKey))
        {
            return apiKey;
        }

        var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(envKey))
        {
            return envKey;
        }

        throw new ModelException(
            "OpenAI API key not found. Please set OPENAI_API_KEY environment variable or provide apiKey parameter. / 未找到 OpenAI API 密钥。请设置 OPENAI_API_KEY 环境变量或提供 apiKey 参数。");
    }
}
