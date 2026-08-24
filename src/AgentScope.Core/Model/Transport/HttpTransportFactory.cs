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
/// Factory for creating IHttpTransport instances based on configuration.
/// Creates an HttpClient-based transport with configurable handler settings,
/// proxy support, timeouts, and default headers.
/// Corresponds to Java: io.agentscope.core.model.transport.HttpTransportFactory
/// 根据配置创建 IHttpTransport 实例的工厂。
/// 创建基于 HttpClient 的传输，支持可配置的处理程序设置、
/// 代理支持、超时和默认标头。
/// 对应 Java: io.agentscope.core.model.transport.HttpTransportFactory
/// </summary>
public static class HttpTransportFactory
{
    /// <summary>
    /// Creates an IHttpTransport instance based on the provided configuration.
    /// Configures HttpClientHandler with proxy, redirect, and connection settings,
    /// then wraps it in an HttpClientTransport.
    /// 根据提供的配置创建 IHttpTransport 实例。
    /// 使用代理、重定向和连接设置配置 HttpClientHandler，
    /// 然后将其包装在 HttpClientTransport 中。
    /// </summary>
    /// <param name="config">
    /// Transport configuration. If null, default configuration is used.
    /// 传输配置。如果为 null，则使用默认配置。
    /// </param>
    /// <returns>
    /// An IHttpTransport instance configured with the specified settings.
    /// 使用指定设置配置的 IHttpTransport 实例。
    /// </returns>
    /// <exception cref="System.NotSupportedException">
    /// Thrown when SOCKS proxy is configured, as the built-in transport only supports HTTP/HTTPS proxies.
    /// 当配置了 SOCKS 代理时抛出，因为内置传输仅支持 HTTP/HTTPS 代理。
    /// </exception>
    public static IHttpTransport Create(HttpTransportConfig? config = null)
    {
        config ??= new HttpTransportConfig();

        var handler = new System.Net.Http.HttpClientHandler
        {
            AllowAutoRedirect = config.AllowAutoRedirect,
            MaxConnectionsPerServer = config.MaxConnectionsPerServer
        };

        if (config.Proxy is { } proxy && !string.IsNullOrEmpty(proxy.Host))
        {
            // System.Net.WebProxy only supports HTTP/HTTPS proxies; SOCKS requires additional handlers
            // (e.g., third-party SOCKS handler). Explicitly fail on SOCKS to avoid silent misconfiguration.
            // System.Net.WebProxy 仅支持 HTTP/HTTPS 代理；SOCKS 需额外处理器（如第三方 SOCKS handler），
            // 此处对 SOCKS 配置显式失败，避免静默误配置。
            if (proxy.Type is ProxyType.Socks4 or ProxyType.Socks4a or ProxyType.Socks5)
            {
                throw new System.NotSupportedException(
                    $"Current HTTP transport does not support {proxy.Type} proxy; configure HTTP/HTTPS proxy or provide a SOCKS-capable handler. " +
                    $"当前 HTTP 传输不支持 {proxy.Type} 代理；请配置 HTTP/HTTPS 代理或提供支持 SOCKS 的处理器。");
            }

            handler.Proxy = new System.Net.WebProxy(proxy.ToProxyUrl())
            {
                UseDefaultCredentials = string.IsNullOrEmpty(proxy.Username)
            };
            handler.UseProxy = true;
        }

        var client = new System.Net.Http.HttpClient(handler)
        {
            Timeout = config.RequestTimeout
        };

        if (!string.IsNullOrEmpty(config.UserAgent))
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd(config.UserAgent);
        }

        foreach (var header in config.DefaultHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        return new HttpClientTransport(client);
    }
}
