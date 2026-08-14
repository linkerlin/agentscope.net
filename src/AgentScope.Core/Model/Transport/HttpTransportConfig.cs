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
/// HTTP 传输配置：超时、HTTP 版本、代理、最大连接、自动重试等。
/// 对应 Java: io.agentscope.core.model.transport.HttpTransportConfig
/// </summary>
public class HttpTransportConfig
{
    /// <summary>连接超时。</summary>
    public System.TimeSpan ConnectTimeout { get; set; } = System.TimeSpan.FromSeconds(30);

    /// <summary>读取（响应）超时。</summary>
    public System.TimeSpan ReadTimeout { get; set; } = System.TimeSpan.FromSeconds(120);

    /// <summary>整体请求超时。</summary>
    public System.TimeSpan RequestTimeout { get; set; } = System.TimeSpan.FromMinutes(10);

    /// <summary>HTTP 协议版本。</summary>
    public HttpVersion HttpVersion { get; set; } = HttpVersion.Http11;

    /// <summary>代理配置（null 表示不使用代理）。</summary>
    public ProxyConfig? Proxy { get; set; }

    /// <summary>每主机最大并发连接。</summary>
    public int MaxConnectionsPerServer { get; set; } = 50;

    /// <summary>是否在底层启用自动重定向跟随。</summary>
    public bool AllowAutoRedirect { get; set; } = true;

    /// <summary>User-Agent。</summary>
    public string? UserAgent { get; set; }

    /// <summary>默认请求头。</summary>
    public System.Collections.Generic.Dictionary<string, string> DefaultHeaders { get; } = new();
}
