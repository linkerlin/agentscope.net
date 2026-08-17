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

namespace AgentScope.Core.Model;

/// <summary>
/// Specialized model HTTP exception that carries HTTP status code and optional response body.
/// Provides a Retryable property to determine if the request can be retried.
/// Corresponds to Java: io.agentscope.core.model.ModelHttpException
/// 模型 HTTP 异常细分：携带 HTTP 状态码与（可能的）响应体。
/// 提供 Retryable 属性以确定请求是否可重试。
/// 对应 Java: io.agentscope.core.model.ModelHttpException
/// </summary>
public class ModelHttpException : ModelException
{
    /// <summary>
    /// Gets the HTTP status code (0 indicates unknown/transport-layer error).
    /// 获取 HTTP 状态码（0 表示未知/传输层错误）。
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Gets the raw response body from the HTTP response (may be null).
    /// 获取 HTTP 响应的原始响应体（可能为空）。
    /// </summary>
    public string? ResponseBody { get; }

    /// <summary>
    /// Gets whether the request is retryable (5xx server errors or 429 rate limit).
    /// 获取请求是否可重试（5xx 服务器错误或 429 速率限制）。
    /// </summary>
    public bool Retryable => ModelUtils.IsRetryableStatus(StatusCode);

    /// <summary>
    /// Initializes a new instance with message and status code.
    /// 使用消息和状态码初始化新实例。
    /// </summary>
    /// <param name="message">Error message / 错误消息。</param>
    /// <param name="statusCode">HTTP status code / HTTP 状态码。</param>
    /// <param name="responseBody">Optional response body / 可选的响应体。</param>
    public ModelHttpException(string message, int statusCode, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Initializes a new instance with message, status code, and inner exception.
    /// 使用消息、状态码和内部异常初始化新实例。
    /// </summary>
    /// <param name="message">Error message / 错误消息。</param>
    /// <param name="statusCode">HTTP status code / HTTP 状态码。</param>
    /// <param name="inner">Inner exception / 内部异常。</param>
    /// <param name="responseBody">Optional response body / 可选的响应体。</param>
    public ModelHttpException(string message, int statusCode, System.Exception inner, string? responseBody = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Initializes a new instance with full diagnostic information.
    /// 使用完整的诊断信息初始化新实例。
    /// </summary>
    /// <param name="message">Error message / 错误消息。</param>
    /// <param name="statusCode">HTTP status code / HTTP 状态码。</param>
    /// <param name="inner">Inner exception / 内部异常。</param>
    /// <param name="modelName">Model name / 模型名称。</param>
    /// <param name="provider">Provider name / 提供程序名称。</param>
    /// <param name="responseBody">Optional response body / 可选的响应体。</param>
    public ModelHttpException(string message, int statusCode, System.Exception inner,
        string modelName, string provider, string? responseBody = null)
        : base(message, inner, modelName, provider)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
