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

using System.Collections.Generic;

namespace AgentScope.Core.Model.Transport.WebSocket;

/// <summary>
/// WebSocket 传输配置：连接超时、心跳、子协议、请求头。
/// 对应 Java: io.agentscope.core.model.transport.websocket.WebSocketTransportConfig
/// </summary>
public class WebSocketTransportConfig
{
    /// <summary>连接超时。</summary>
    public System.TimeSpan ConnectTimeout { get; set; } = System.TimeSpan.FromSeconds(30);

    /// <summary>心跳/Ping 间隔（0 表示禁用）。</summary>
    public System.TimeSpan KeepAliveInterval { get; set; } = TransportConstants.DefaultWebSocketPingInterval;

    /// <summary>最大接收消息字节数。</summary>
    public int ReceiveBufferSize { get; set; } = 16 * 1024;

    /// <summary>子协议列表。</summary>
    public List<string> SubProtocols { get; } = new();

    /// <summary>请求头。</summary>
    public Dictionary<string, string> Headers { get; } = new();

    /// <summary>是否启用压缩。</summary>
    public bool EnableCompression { get; set; } = false;
}
