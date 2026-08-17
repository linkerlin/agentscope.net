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
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Core.Model.Transport;

/// <summary>
/// HTTP transport implementation using .NET HttpClient for the AgentScope framework.
/// Supports both standard request/response and SSE streaming patterns with configurable
/// timeouts, headers, and cancellation.
/// Corresponds to Java: io.agentscope.core.model.transport.HttpClientTransport
/// 使用 .NET HttpClient 的 HTTP 传输实现，用于 AgentScope 框架。
/// 支持标准请求/响应和 SSE 流式传输模式，可配置超时、标头和取消。
/// 对应 Java: io.agentscope.core.model.transport.HttpClientTransport
/// </summary>
public class HttpClientTransport : IHttpTransport, IDisposable
{
    /// <summary>
    /// The underlying HttpClient instance used for all HTTP operations.
    /// 用于所有 HTTP 操作的底层 HttpClient 实例。
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Flag indicating whether the transport has been disposed.
    /// 指示传输是否已释放的标志。
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of HttpClientTransport with a default HttpClient.
    /// Sets a default User-Agent header for AgentScope.NET.
    /// 使用默认 HttpClient 初始化 HttpClientTransport 的新实例。
    /// 为 AgentScope.NET 设置默认的 User-Agent 标头。
    /// </summary>
    public HttpClientTransport()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "AgentScope.NET/1.0");
    }

    /// <summary>
    /// Initializes a new instance of HttpClientTransport with a custom HttpClient.
    /// Allows injection of a pre-configured HttpClient (e.g., with custom handlers, pooling, or proxy settings).
    /// 使用自定义 HttpClient 初始化 HttpClientTransport 的新实例。
    /// 允许注入预配置的 HttpClient（例如，带有自定义处理程序、连接池或代理设置）。
    /// </summary>
    /// <param name="httpClient">The custom HttpClient instance to use / 要使用的自定义 HttpClient 实例。</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient is null / 当 httpClient 为 null 时抛出。</exception>
    public HttpClientTransport(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<HttpResponse> ExecuteAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        // Build the HttpRequestMessage from our transport-layer HttpRequest
        // 从传输层 HttpRequest 构建 HttpRequestMessage
        using var httpRequest = new HttpRequestMessage(
            new HttpMethod(request.Method),
            request.Url);

        // Copy all request headers to the HttpRequestMessage
        // 将所有请求标头复制到 HttpRequestMessage
        foreach (var header in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Add JSON body if present
        // 如果存在请求体，添加 JSON 正文
        if (!string.IsNullOrEmpty(request.Body))
        {
            httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
        }

        // Apply timeout: use request timeout or default 60 seconds
        // 应用超时：使用请求超时或默认 60 秒
        var timeout = request.Timeout ?? TimeSpan.FromSeconds(60);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            // Send the request with ResponseHeadersRead to start streaming headers early
            // 使用 ResponseHeadersRead 发送请求以尽早开始流式传输标头
            using var httpResponse = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            // Read the full response body as string
            // 将完整响应正文读取为字符串
            var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

            // Collect response headers into a dictionary
            // 将响应标头收集到字典中
            var headers = new Dictionary<string, string>();
            foreach (var header in httpResponse.Headers)
            {
                headers[header.Key] = string.Join(",", header.Value);
            }

            return new HttpResponse
            {
                StatusCode = (int)httpResponse.StatusCode,
                Headers = headers,
                Body = body
            };
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred (not user cancellation)
            // 发生超时（不是用户取消）
            throw new TimeoutException($"Request to {request.Url} timed out after {timeout}");
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamAsync(
        HttpRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Build the HttpRequestMessage for streaming
        // 为流式传输构建 HttpRequestMessage
        using var httpRequest = new HttpRequestMessage(
            new HttpMethod(request.Method),
            request.Url);

        // Copy all request headers
        // 复制所有请求标头
        foreach (var header in request.Headers)
        {
            httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Add JSON body if present
        // 如果存在请求体，添加 JSON 正文
        if (!string.IsNullOrEmpty(request.Body))
        {
            httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
        }

        // Apply timeout
        // 应用超时
        var timeout = request.Timeout ?? TimeSpan.FromSeconds(60);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        HttpResponseMessage? httpResponse = null;
        try
        {
            // Send request and get response stream
            // 发送请求并获取响应流
            httpResponse = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            httpResponse.EnsureSuccessStatusCode();

            // Read the response stream line by line (SSE format)
            // 逐行读取响应流（SSE 格式）
            var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream && !cts.Token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (!string.IsNullOrEmpty(line))
                {
                    yield return line;
                }
            }
        }
        finally
        {
            httpResponse?.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
