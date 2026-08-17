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

namespace AgentScope.Core.Model.Transport;

/// <summary>
/// HTTP request encapsulation for the transport layer in the AgentScope framework.
/// Uses C# 12 primary constructor syntax with required/init properties for immutability.
/// Corresponds to Java: io.agentscope.core.model.transport.HttpRequest
/// AgentScope 框架中传输层的 HTTP 请求封装。
/// 使用 C# 12 主构造函数语法，通过 required/init 属性实现不可变性。
/// 对应 Java: io.agentscope.core.model.transport.HttpRequest
/// </summary>
public class HttpRequest
{
    /// <summary>
    /// The target URL for the HTTP request.
    /// HTTP 请求的目标 URL。
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// The HTTP method (e.g., GET, POST, PUT, DELETE).
    /// HTTP 方法（例如 GET、POST、PUT、DELETE）。
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// Dictionary of HTTP request headers (key-value pairs).
    /// HTTP 请求标头字典（键值对）。
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = new();

    /// <summary>
    /// Optional request body content (typically JSON string).
    /// 可选的请求正文内容（通常为 JSON 字符串）。
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Optional request timeout. If null, a default timeout will be applied by the transport.
    /// 可选的请求超时。如果为 null，传输层将应用默认超时。
    /// </summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>
/// Builder pattern implementation for constructing HttpRequest instances with a fluent API.
/// Provides a convenient way to build complex HTTP requests with method chaining.
/// Corresponds to Java: io.agentscope.core.model.transport.HttpRequestBuilder
/// 用于通过流畅 API 构建 HttpRequest 实例的构建器模式实现。
/// 提供了一种通过方法链构建复杂 HTTP 请求的便捷方式。
/// 对应 Java: io.agentscope.core.model.transport.HttpRequestBuilder
/// </summary>
public class HttpRequestBuilder
{
    /// <summary>Request URL / 请求 URL</summary>
    private string? _url;

    /// <summary>HTTP method, defaults to GET / HTTP 方法，默认为 GET</summary>
    private string _method = "GET";

    /// <summary>Request headers dictionary / 请求标头字典</summary>
    private readonly Dictionary<string, string> _headers = new();

    /// <summary>Request body / 请求正文</summary>
    private string? _body;

    /// <summary>Request timeout / 请求超时</summary>
    private TimeSpan? _timeout;

    /// <summary>
    /// Sets the request URL.
    /// 设置请求 URL。
    /// </summary>
    /// <param name="url">The target URL / 目标 URL。</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例。</returns>
    public HttpRequestBuilder Url(string url)
    {
        _url = url;
        return this;
    }

    /// <summary>
    /// Sets the HTTP method (GET, POST, PUT, DELETE, etc.).
    /// 设置 HTTP 方法（GET、POST、PUT、DELETE 等）。
    /// </summary>
    /// <param name="method">The HTTP method / HTTP 方法。</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例。</returns>
    public HttpRequestBuilder Method(string method)
    {
        _method = method;
        return this;
    }

    /// <summary>
    /// Adds a single HTTP header.
    /// 添加单个 HTTP 标头。
    /// </summary>
    /// <param name="name">Header name / 标头名称。</param>
    /// <param name="value">Header value / 标头值。</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例。</returns>
    public HttpRequestBuilder Header(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple HTTP headers from a dictionary.
    /// 从字典中添加多个 HTTP 标头。
    /// </summary>
    /// <param name="headers">Dictionary of headers to add / 要添加的标头字典。</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例。</returns>
    public HttpRequestBuilder Headers(Dictionary<string, string> headers)
    {
        foreach (var kvp in headers)
        {
            _headers[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// Sets the request body (typically a JSON string).
    /// 设置请求正文（通常为 JSON 字符串）。
    /// </summary>
    /// <param name="body">The request body / 请求正文。</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例。</returns>
    public HttpRequestBuilder Body(string body)
    {
        _body = body;
        return this;
    }

    /// <summary>
    /// Sets the request timeout.
    /// 设置请求超时。
    /// </summary>
    /// <param name="timeout">The timeout duration / 超时持续时间。</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例。</returns>
    public HttpRequestBuilder Timeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Builds the HttpRequest instance with the configured parameters.
    /// Validates that the URL is provided before building.
    /// 使用配置的参数构建 HttpRequest 实例。
    /// 在构建前验证是否提供了 URL。
    /// </summary>
    /// <returns>The constructed HttpRequest instance / 构建的 HttpRequest 实例。</returns>
    /// <exception cref="ArgumentException">Thrown when URL is null or empty / 当 URL 为 null 或空时抛出。</exception>
    public HttpRequest Build()
    {
        if (string.IsNullOrEmpty(_url))
        {
            throw new ArgumentException("URL is required");
        }

        return new HttpRequest
        {
            Url = _url,
            Method = _method,
            Headers = new Dictionary<string, string>(_headers),
            Body = _body,
            Timeout = _timeout
        };
    }
}
