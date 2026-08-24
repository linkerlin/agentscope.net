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

namespace AgentScope.Core.Model.Transport;

/// <summary>
/// Exception thrown when an HTTP transport operation fails in the AgentScope framework.
/// Carries optional HTTP status code and response body for detailed error reporting.
/// Corresponds to Java: io.agentscope.core.model.transport.HttpTransportException
/// 当 AgentScope 框架中的 HTTP 传输操作失败时抛出的异常。
/// 携带可选的 HTTP 状态码和响应正文，用于详细的错误报告。
/// 对应 Java: io.agentscope.core.model.transport.HttpTransportException
/// </summary>
public class HttpTransportException : System.Exception
{
    /// <summary>
    /// The HTTP status code associated with the error, if available.
    /// 与错误关联的 HTTP 状态码（如果可用）。
    /// </summary>
    public int? StatusCode { get; }

    /// <summary>
    /// The response body content associated with the error, if available.
    /// 与错误关联的响应正文内容（如果可用）。
    /// </summary>
    public string? ResponseBody { get; }

    /// <summary>
    /// Initializes a new instance with just an error message.
    /// 仅使用错误消息初始化新实例。
    /// </summary>
    /// <param name="message">The error message / 错误消息。</param>
    public HttpTransportException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with an error message and inner exception.
    /// 使用错误消息和内部异常初始化新实例。
    /// </summary>
    /// <param name="message">The error message / 错误消息。</param>
    /// <param name="inner">The inner exception / 内部异常。</param>
    public HttpTransportException(string message, System.Exception inner) : base(message, inner) { }

    /// <summary>
    /// Initializes a new instance with an error message, HTTP status code, and optional response body.
    /// 使用错误消息、HTTP 状态码和可选的响应正文初始化新实例。
    /// </summary>
    /// <param name="message">The error message / 错误消息。</param>
    /// <param name="statusCode">The HTTP status code / HTTP 状态码。</param>
    /// <param name="responseBody">Optional response body / 可选的响应正文。</param>
    public HttpTransportException(string message, int statusCode, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
