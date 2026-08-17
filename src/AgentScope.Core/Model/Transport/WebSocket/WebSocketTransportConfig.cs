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

namespace AgentScope.Core.Model.Transport.WebSocket;

/// <summary>
/// WebSocket transport configuration: connection timeout, heartbeat, sub-protocols, headers.
/// WebSocket 传输配置：连接超时、心跳、子协议、请求头。
/// Corresponds to Java: io.agentscope.core.model.transport.websocket.WebSocketTransportConfig
/// 对应 Java: io.agentscope.core.model.transport.websocket.WebSocketTransportConfig
/// </summary>
public class WebSocketTransportConfig
{
    /// <summary>
    /// Connection timeout duration. Default is 30 seconds.
    /// 连接超时时间。默认 30 秒。
    /// </summary>
    public System.TimeSpan ConnectTimeout { get; set; } = System.TimeSpan.FromSeconds(30);

    /// <summary>
    /// Keep-alive / ping interval. Set to TimeSpan.Zero to disable.
    /// 心跳/Ping 间隔。设置为 TimeSpan.Zero 表示禁用。
    /// </summary>
    public System.TimeSpan KeepAliveInterval { get; set; } = TransportConstants.DefaultWebSocketPingInterval;

    /// <summary>
    /// Maximum receive buffer size in bytes. Default is 16 KB.
    /// 最大接收缓冲区大小（字节）。默认 16 KB。
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 16 * 1024;

    /// <summary>
    /// List of sub-protocols to negotiate for the WebSocket connection.
    /// WebSocket 连接要协商的子协议列表。
    /// </summary>
    public List<string> SubProtocols { get; } = new();

    /// <summary>
    /// Custom HTTP headers to include in the WebSocket upgrade request.
    /// WebSocket 升级请求中包含的自定义 HTTP 请求头。
    /// </summary>
    public Dictionary<string, string> Headers { get; } = new();

    /// <summary>
    /// Whether to enable per-message compression. Default is false.
    /// 是否启用逐消息压缩。默认禁用。
    /// </summary>
    public bool EnableCompression { get; set; } = false;
}
