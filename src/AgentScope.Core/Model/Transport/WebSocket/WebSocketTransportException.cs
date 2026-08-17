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
/// Exception for WebSocket transport errors.
/// WebSocket 传输异常。
/// Corresponds to Java: io.agentscope.core.model.transport.websocket.WebSocketTransportException
/// 对应 Java: io.agentscope.core.model.transport.websocket.WebSocketTransportException
/// </summary>
public class WebSocketTransportException : System.Exception
{
    /// <summary>
    /// WebSocket close status code, if applicable.
    /// WebSocket 关闭状态码（若适用）。
    /// </summary>
    public int? CloseStatus { get; }

    /// <summary>
    /// Creates a new exception with the specified error message.
    /// 使用指定的错误消息创建新异常。
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    public WebSocketTransportException(string message) : base(message) { }

    /// <summary>
    /// Creates a new exception with the specified error message and inner exception.
    /// 使用指定的错误消息和内部异常创建新异常。
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    /// <param name="inner">Inner exception / 内部异常</param>
    public WebSocketTransportException(string message, System.Exception inner) : base(message, inner) { }

    /// <summary>
    /// Creates a new exception with the specified error message and close status code.
    /// 使用指定的错误消息和关闭状态码创建新异常。
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    /// <param name="closeStatus">WebSocket close status code / WebSocket 关闭状态码</param>
    public WebSocketTransportException(string message, int closeStatus) : base(message)
    {
        CloseStatus = closeStatus;
    }
}
