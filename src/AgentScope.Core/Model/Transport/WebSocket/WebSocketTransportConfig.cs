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
/// WebSocket transport configuration: connection timeout, keep-alive interval,
/// receive buffer size, sub-protocols, custom headers, and compression settings.
/// Corresponds to Java: io.agentscope.core.model.transport.websocket.WebSocketTransportConfig
/// WebSocket 传输配置：连接超时、心跳间隔、
/// 接收缓冲区大小、子协议、自定义请求头和压缩设置。
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
    /// Keep-alive / ping interval for maintaining the WebSocket connection.
    /// Set to TimeSpan.Zero to disable keep-alive pings.
    /// 保持 WebSocket 连接活跃的心跳/Ping 间隔。
    /// 设置为 TimeSpan.Zero 表示禁用心跳。
    /// </summary>
    public System.TimeSpan KeepAliveInterval { get; set; } = TransportConstants.DefaultWebSocketPingInterval;

    /// <summary>
    /// Maximum receive buffer size in bytes. Default is 16 KB (16 * 1024).
    /// Larger buffers reduce fragmentation for big messages but use more memory.
    /// 最大接收缓冲区大小（字节）。默认 16 KB（16 * 1024）。
    /// 较大的缓冲区可减少大消息的分片，但会使用更多内存。
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 16 * 1024;

    /// <summary>
    /// List of sub-protocols to negotiate for the WebSocket connection.
    /// The server will select one of these protocols during the handshake.
    /// WebSocket 连接要协商的子协议列表。
    /// 服务器将在握手期间选择其中一个协议。
    /// </summary>
    public List<string> SubProtocols { get; } = new();

    /// <summary>
    /// Custom HTTP headers to include in the WebSocket upgrade request.
    /// These are sent during the initial HTTP-to-WebSocket upgrade handshake.
    /// WebSocket 升级请求中包含的自定义 HTTP 请求头。
    /// 这些头在初始 HTTP 到 WebSocket 的升级握手期间发送。
    /// </summary>
    public Dictionary<string, string> Headers { get; } = new();

    /// <summary>
    /// Whether to enable per-message compression (deflate extension). Default is false.
    /// When enabled, messages are compressed using the WebSocket per-message deflate extension.
    /// 是否启用逐消息压缩（deflate 扩展）。默认禁用。
    /// 启用后，消息将使用 WebSocket 逐消息 deflate 扩展进行压缩。
    /// </summary>
    public bool EnableCompression { get; set; } = false;
}
