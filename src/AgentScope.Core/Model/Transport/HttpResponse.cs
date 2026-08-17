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

namespace AgentScope.Core.Model.Transport;

/// <summary>
/// HTTP response encapsulation for the transport layer in the AgentScope framework.
/// Contains the status code, headers, and body returned from an HTTP request.
/// Provides a convenience property to check if the response indicates success (2xx).
/// Corresponds to Java: io.agentscope.core.model.transport.HttpResponse
/// AgentScope 框架中传输层的 HTTP 响应封装。
/// 包含 HTTP 请求返回的状态码、标头和正文。
/// 提供便捷属性来检查响应是否表示成功（2xx）。
/// 对应 Java: io.agentscope.core.model.transport.HttpResponse
/// </summary>
public class HttpResponse
{
    /// <summary>
    /// The HTTP status code (e.g., 200 for OK, 404 for Not Found, 500 for Server Error).
    /// HTTP 状态码（例如 200 表示成功、404 表示未找到、500 表示服务器错误）。
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// Dictionary of response headers returned by the server.
    /// 服务器返回的响应标头字典。
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = new();

    /// <summary>
    /// The response body content as a string (typically JSON).
    /// 响应正文内容（通常为 JSON 字符串）。
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Gets whether the HTTP response indicates success (status code in the 2xx range).
    /// 获取 HTTP 响应是否表示成功（状态码在 2xx 范围内）。
    /// </summary>
    public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode < 300;
}
