// Copyright 2024-2026 the original author or authors.
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
/// HTTP 传输工厂：根据配置创建 <see cref="IHttpTransport"/> 实例。
/// 对应 Java: io.agentscope.core.model.transport.HttpTransportFactory
/// </summary>
public static class HttpTransportFactory
{
    /// <summary>
    /// 创建一个基于 HttpClient 的 HTTP 传输实例。
    /// </summary>
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
            // System.Net.WebProxy 仅支持 HTTP/HTTPS 代理；SOCKS 需额外处理器（如第三方 SOCKS handler），
            // 此处对 SOCKS 配置显式失败，避免静默误配置。
            if (proxy.Type is ProxyType.Socks4 or ProxyType.Socks4a or ProxyType.Socks5)
            {
                throw new System.NotSupportedException(
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
