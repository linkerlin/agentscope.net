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

namespace AgentScope.Extensions.Nacos;

/// <summary>
/// Nacos A2A registry configuration options.
/// Corresponds to the Java NacosA2aRegistryProperties; injected via the IOptions pattern.
/// Nacos A2A 注册表配置选项。对标 Java NacosA2aRegistryProperties，通过 IOptions 模式注入。
/// </summary>
public sealed class NacosA2aRegistryOptions
{
    /// <summary>
    /// Nacos server address (including protocol and port).
    /// Nacos 服务器地址（包含协议和端口）。
    /// </summary>
    public string ServerAddr { get; set; } = "http://localhost:8848";

    /// <summary>
    /// Nacos namespace ID; empty string means the public namespace.
    /// Nacos 命名空间 ID，空字符串表示公共命名空间。
    /// </summary>
    public string Namespace { get; set; } = "";

    /// <summary>
    /// Nacos group name for the registered services.
    /// Nacos 分组名称，用于归组已注册的服务。
    /// </summary>
    public string GroupName { get; set; } = "DEFAULT_GROUP";

    /// <summary>
    /// Optional username for Nacos authentication.
    /// 可选的 Nacos 认证用户名。
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Optional password for Nacos authentication.
    /// 可选的 Nacos 认证密码。
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Interval between heartbeat pings to Nacos. Defaults to 5 seconds.
    /// 向 Nacos 发送心跳的间隔时间，默认为 5 秒。
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
}
