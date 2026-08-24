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

namespace AgentScope.Core.Model.Transport.WebSocket;

/// <summary>
/// WebSocket connection request parameters containing the target URI, optional HTTP headers,
/// and optional sub-protocol for the WebSocket upgrade handshake.
/// Corresponds to Java: io.agentscope.core.model.transport.websocket.WebSocketRequest
/// WebSocket 连接请求参数，包含目标 URI、可选的 HTTP 请求头
/// 以及可选的 WebSocket 升级握手子协议。
/// 对应 Java: io.agentscope.core.model.transport.websocket.WebSocketRequest
/// </summary>
public class WebSocketRequest
{
    /// <summary>
    /// The target WebSocket server URI to connect to (e.g., ws://host:port/path or wss://host:port/path).
    /// 要连接的目标 WebSocket 服务器 URI（例如 ws://host:port/path 或 wss://host:port/path）。
    /// </summary>
    public Uri Uri { get; set; } = null!;

    /// <summary>
    /// Optional HTTP headers to include in the WebSocket upgrade request.
    /// These are sent during the initial HTTP upgrade handshake.
    /// 可选，WebSocket 升级请求中包含的 HTTP 请求头。
    /// 这些头在初始 HTTP 升级握手期间发送。
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Optional sub-protocol for the WebSocket connection (e.g., "json", "protocol-buffers").
    /// Used for protocol negotiation during the WebSocket handshake.
    /// 可选，WebSocket 连接的子协议（例如 "json"、"protocol-buffers"）。
    /// 用于 WebSocket 握手期间的协议协商。
    /// </summary>
    public string? SubProtocol { get; set; }
}
