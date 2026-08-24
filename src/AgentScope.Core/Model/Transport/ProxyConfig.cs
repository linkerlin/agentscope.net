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
/// Enumeration of supported proxy types for the HTTP transport layer.
/// Supports HTTP, HTTPS, and SOCKS (4, 4a, 5) proxies.
/// Corresponds to Java: io.agentscope.core.model.transport.ProxyType
/// HTTP 传输层支持的代理类型枚举。
/// 支持 HTTP、HTTPS 和 SOCKS（4、4a、5）代理。
/// 对应 Java: io.agentscope.core.model.transport.ProxyType
/// </summary>
public enum ProxyType
{
    /// <summary>HTTP proxy / HTTP 代理。</summary>
    Http,

    /// <summary>HTTPS proxy (SSL/TLS) / HTTPS 代理（SSL/TLS）。</summary>
    Https,

    /// <summary>SOCKS4 proxy / SOCKS4 代理。</summary>
    Socks4,

    /// <summary>SOCKS4a proxy (with domain name resolution) / SOCKS4a 代理（支持域名解析）。</summary>
    Socks4a,

    /// <summary>SOCKS5 proxy (with authentication support) / SOCKS5 代理（支持认证）。</summary>
    Socks5
}

/// <summary>
/// Configuration for HTTP proxy in the transport layer.
/// Specifies proxy type, host, port, optional authentication, and non-proxy hosts.
/// Corresponds to Java: io.agentscope.core.model.transport.ProxyConfig
/// 传输层中 HTTP 代理的配置。
/// 指定代理类型、主机、端口、可选认证和不经过代理的主机列表。
/// 对应 Java: io.agentscope.core.model.transport.ProxyConfig
/// </summary>
public class ProxyConfig
{
    /// <summary>
    /// The proxy type (HTTP, HTTPS, SOCKS4, SOCKS4a, SOCKS5). Default is HTTP.
    /// 代理类型（HTTP、HTTPS、SOCKS4、SOCKS4a、SOCKS5）。默认为 HTTP。
    /// </summary>
    public ProxyType Type { get; set; } = ProxyType.Http;

    /// <summary>
    /// The proxy hostname or IP address.
    /// 代理主机名或 IP 地址。
    /// </summary>
    public string Host { get; set; } = "";

    /// <summary>
    /// The proxy port number. Default is 8080.
    /// 代理端口号。默认为 8080。
    /// </summary>
    public int Port { get; set; } = 8080;

    /// <summary>
    /// Optional username for proxy authentication.
    /// 用于代理认证的可选用户名。
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Optional password for proxy authentication.
    /// 用于代理认证的可选密码。
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// List of hosts/domains that should bypass the proxy.
    /// 应绕过代理的主机/域名列表。
    /// </summary>
    public System.Collections.Generic.List<string> NonProxyHosts { get; } = new();

    /// <summary>
    /// Initializes a new instance of ProxyConfig with default values.
    /// 使用默认值初始化 ProxyConfig 的新实例。
    /// </summary>
    public ProxyConfig() { }

    /// <summary>
    /// Initializes a new instance of ProxyConfig with specified host, port, and type.
    /// 使用指定的主机、端口和类型初始化 ProxyConfig 的新实例。
    /// </summary>
    /// <param name="host">The proxy host / 代理主机。</param>
    /// <param name="port">The proxy port / 代理端口。</param>
    /// <param name="type">The proxy type (default HTTP) / 代理类型（默认 HTTP）。</param>
    public ProxyConfig(string host, int port, ProxyType type = ProxyType.Http)
    {
        Host = host;
        Port = port;
        Type = type;
    }

    /// <summary>
    /// Constructs the proxy URL string (e.g., http://host:port).
    /// Includes authentication credentials if username is provided.
    /// 构造代理 URL 字符串（例如 http://host:port）。
    /// 如果提供了用户名，则包含认证凭据。
    /// </summary>
    /// <returns>
    /// The proxy URL in format scheme://host:port or scheme://user:pass@host:port.
    /// 格式为 scheme://host:port 或 scheme://user:pass@host:port 的代理 URL。
    /// </returns>
    public string ToProxyUrl()
    {
        var scheme = Type switch
        {
            ProxyType.Socks4 or ProxyType.Socks4a => "socks4",
            ProxyType.Socks5 => "socks5",
            ProxyType.Https => "https",
            _ => "http"
        };

        if (!string.IsNullOrEmpty(Username))
        {
            return $"{scheme}://{Username}:{Password}@{Host}:{Port}";
        }

        return $"{scheme}://{Host}:{Port}";
    }
}
