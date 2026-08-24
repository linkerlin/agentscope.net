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
/// Configuration for HTTP transport in the AgentScope framework.
/// Controls timeouts, HTTP version, proxy settings, max connections, auto-redirect, and default headers.
/// Corresponds to Java: io.agentscope.core.model.transport.HttpTransportConfig
/// AgentScope 框架中 HTTP 传输的配置。
/// 控制超时、HTTP 版本、代理设置、最大连接数、自动重定向和默认标头。
/// 对应 Java: io.agentscope.core.model.transport.HttpTransportConfig
/// </summary>
public class HttpTransportConfig
{
    /// <summary>
    /// Connection timeout for establishing TCP connections. Default is 30 seconds.
    /// 建立 TCP 连接的连接超时。默认 30 秒。
    /// </summary>
    public System.TimeSpan ConnectTimeout { get; set; } = System.TimeSpan.FromSeconds(30);

    /// <summary>
    /// Read timeout for receiving response data. Default is 120 seconds.
    /// 接收响应数据的读取超时。默认 120 秒。
    /// </summary>
    public System.TimeSpan ReadTimeout { get; set; } = System.TimeSpan.FromSeconds(120);

    /// <summary>
    /// Overall request timeout including connection and data transfer. Default is 10 minutes.
    /// 包括连接和数据传输的整体请求超时。默认 10 分钟。
    /// </summary>
    public System.TimeSpan RequestTimeout { get; set; } = System.TimeSpan.FromMinutes(10);

    /// <summary>
    /// HTTP protocol version to use (e.g., HTTP/1.1, HTTP/2, HTTP/3). Default is HTTP/1.1.
    /// 要使用的 HTTP 协议版本（例如 HTTP/1.1、HTTP/2、HTTP/3）。默认 HTTP/1.1。
    /// </summary>
    public HttpVersion HttpVersion { get; set; } = HttpVersion.Http11;

    /// <summary>
    /// Proxy configuration. Set to null to use no proxy (direct connection).
    /// 代理配置。设置为 null 表示不使用代理（直接连接）。
    /// </summary>
    public ProxyConfig? Proxy { get; set; }

    /// <summary>
    /// Maximum number of concurrent connections per server. Default is 50.
    /// 每台服务器的最大并发连接数。默认 50。
    /// </summary>
    public int MaxConnectionsPerServer { get; set; } = 50;

    /// <summary>
    /// Whether to automatically follow HTTP redirect responses. Default is true.
    /// 是否自动跟随 HTTP 重定向响应。默认 true。
    /// </summary>
    public bool AllowAutoRedirect { get; set; } = true;

    /// <summary>
    /// Custom User-Agent header value. If null, a default value will be used.
    /// 自定义 User-Agent 标头值。如果为 null，将使用默认值。
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Default HTTP headers to include in every request.
    /// 要包含在每个请求中的默认 HTTP 标头。
    /// </summary>
    public System.Collections.Generic.Dictionary<string, string> DefaultHeaders { get; } = new();
}
