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
/// Transport layer constants: default timeouts, content types, authentication headers, etc.
/// Centralizes all magic strings and default values used by the HTTP/WebSocket transport layer.
/// Corresponds to Java: io.agentscope.core.model.transport.TransportConstants
/// 传输层常量：默认超时、内容类型、认证头等。
/// 集中管理 HTTP/WebSocket 传输层使用的所有魔数字符串和默认值。
/// 对应 Java: io.agentscope.core.model.transport.TransportConstants
/// </summary>
public static class TransportConstants
{
    /// <summary>
    /// Content-Type header value for JSON requests.
    /// JSON 请求的 Content-Type 头值。
    /// </summary>
    public const string ContentTypeJson = "application/json";

    /// <summary>
    /// Content-Type header value for form URL-encoded data.
    /// 表单 URL 编码数据的 Content-Type 头值。
    /// </summary>
    public const string ContentTypeForm = "application/x-www-form-urlencoded";

    /// <summary>
    /// Content-Type header value for Server-Sent Events (SSE) streaming.
    /// 服务器推送事件（SSE）流式传输的 Content-Type 头值。
    /// </summary>
    public const string ContentTypeEventStream = "text/event-stream";

    /// <summary>
    /// HTTP Authorization header name.
    /// HTTP 认证头名称。
    /// </summary>
    public const string Authorization = "Authorization";

    /// <summary>
    /// Prefix for Bearer token in the Authorization header (e.g., "Bearer sk-...").
    /// 认证头中 Bearer 令牌的前缀（例如 "Bearer sk-..."）。
    /// </summary>
    public const string BearerPrefix = "Bearer ";

    /// <summary>
    /// Alternative API key header name used by some providers (e.g., Azure).
    /// 某些提供商使用的备选 API 密钥头名称（例如 Azure）。
    /// </summary>
    public const string ApiKeyHeader = "api-key";

    /// <summary>
    /// Default User-Agent string for HTTP requests.
    /// HTTP 请求的默认 User-Agent 字符串。
    /// </summary>
    public const string UserAgent = "AgentScope.NET/1.0";

    /// <summary>
    /// Default connection timeout (30 seconds).
    /// 默认连接超时（30 秒）。
    /// </summary>
    public static readonly System.TimeSpan DefaultConnectTimeout = System.TimeSpan.FromSeconds(30);

    /// <summary>
    /// Default read timeout (120 seconds).
    /// 默认读取超时（120 秒）。
    /// </summary>
    public static readonly System.TimeSpan DefaultReadTimeout = System.TimeSpan.FromSeconds(120);

    /// <summary>
    /// Default WebSocket ping interval (20 seconds) for keep-alive.
    /// WebSocket 默认心跳间隔（20 秒），用于保持连接活跃。
    /// </summary>
    public static readonly System.TimeSpan DefaultWebSocketPingInterval = System.TimeSpan.FromSeconds(20);

    /// <summary>
    /// Prefix for data lines in Server-Sent Events (SSE) streams.
    /// SSE 流中数据行的前缀标记。
    /// </summary>
    public const string SseDataPrefix = "data: ";

    /// <summary>
    /// Marker indicating the end of an SSE stream (sent by some providers like OpenAI).
    /// SSE 流结束标记（某些提供商如 OpenAI 会发送此标记）。
    /// </summary>
    public const string SseDoneMarker = "[DONE]";
}
