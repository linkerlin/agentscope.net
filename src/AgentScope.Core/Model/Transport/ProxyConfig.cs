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
/// 代理类型枚举。对应 Java: io.agentscope.core.model.transport.ProxyType
/// </summary>
public enum ProxyType
{
    Http,
    Https,
    Socks4,
    Socks4a,
    Socks5
}

/// <summary>
/// HTTP 代理配置。对应 Java: io.agentscope.core.model.transport.ProxyConfig
/// </summary>
public class ProxyConfig
{
    public ProxyType Type { get; set; } = ProxyType.Http;

    /// <summary>代理主机。</summary>
    public string Host { get; set; } = "";

    /// <summary>代理端口。</summary>
    public int Port { get; set; } = 8080;

    /// <summary>用户名（可选）。</summary>
    public string? Username { get; set; }

    /// <summary>密码（可选）。</summary>
    public string? Password { get; set; }

    /// <summary>不经过代理的主机/域名列表。</summary>
    public System.Collections.Generic.List<string> NonProxyHosts { get; } = new();

    public ProxyConfig() { }

    public ProxyConfig(string host, int port, ProxyType type = ProxyType.Http)
    {
        Host = host;
        Port = port;
        Type = type;
    }

    /// <summary>构造代理 URL（如 http://host:port）。</summary>
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
