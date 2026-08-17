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
/// WebSocket connection request parameters.
/// WebSocket 连接请求参数。
/// </summary>
public class WebSocketRequest
{
    /// <summary>
    /// The target WebSocket server URI to connect to.
    /// 要连接的目标 WebSocket 服务器 URI。
    /// </summary>
    public Uri Uri { get; set; } = null!;

    /// <summary>
    /// Optional HTTP headers to include in the WebSocket upgrade request.
    /// 可选，WebSocket 升级请求中包含的 HTTP 请求头。
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Optional sub-protocol for the WebSocket connection.
    /// 可选，WebSocket 连接的子协议。
    /// </summary>
    public string? SubProtocol { get; set; }
}
