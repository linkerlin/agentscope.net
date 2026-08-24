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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Core.Model.Transport;

/// <summary>
/// HTTP transport layer interface for making HTTP requests in the AgentScope framework.
/// Abstracts the actual HTTP client implementation to support different transport mechanisms
/// (standard HttpClient, WebSocket, SSE streaming, etc.).
/// Corresponds to Java: io.agentscope.core.model.transport.HttpTransport
/// AgentScope 框架中用于发起 HTTP 请求的 HTTP 传输层接口。
/// 抽象实际的 HTTP 客户端实现，以支持不同的传输机制
/// （标准 HttpClient、WebSocket、SSE 流式传输等）。
/// 对应 Java: io.agentscope.core.model.transport.HttpTransport
/// </summary>
public interface IHttpTransport
{
    /// <summary>
    /// Executes an HTTP request and returns the response asynchronously.
    /// Supports standard request/response patterns with configurable timeout and headers.
    /// 异步执行 HTTP 请求并返回响应。
    /// 支持标准请求/响应模式，可配置超时和标头。
    /// </summary>
    /// <param name="request">The HTTP request to execute / 要执行的 HTTP 请求。</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌。</param>
    /// <returns>The HTTP response containing status code, headers, and body / 包含状态码、标头和正文的 HTTP 响应。</returns>
    Task<HttpResponse> ExecuteAsync(HttpRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a streaming HTTP request and returns an async enumerable of SSE data lines.
    /// Used for Server-Sent Events (SSE) streaming from AI model APIs.
    /// 执行流式 HTTP 请求并返回 SSE 数据行的异步可枚举序列。
    /// 用于 AI 模型 API 的服务器发送事件（SSE）流式传输。
    /// </summary>
    /// <param name="request">The HTTP request to execute / 要执行的 HTTP 请求。</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌。</param>
    /// <returns>Async enumerable of SSE data lines / SSE 数据行的异步可枚举序列。</returns>
    IAsyncEnumerable<string> StreamAsync(HttpRequest request, CancellationToken cancellationToken = default);
}
