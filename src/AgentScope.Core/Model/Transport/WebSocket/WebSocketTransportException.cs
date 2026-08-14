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

namespace AgentScope.Core.Model.Transport.WebSocket;

/// <summary>
/// WebSocket 传输异常。对应 Java: io.agentscope.core.model.transport.websocket.WebSocketTransportException
/// </summary>
public class WebSocketTransportException : System.Exception
{
    /// <summary>WebSocket 关闭状态码（若适用）。</summary>
    public int? CloseStatus { get; }

    public WebSocketTransportException(string message) : base(message) { }

    public WebSocketTransportException(string message, System.Exception inner) : base(message, inner) { }

    public WebSocketTransportException(string message, int closeStatus) : base(message)
    {
        CloseStatus = closeStatus;
    }
}
