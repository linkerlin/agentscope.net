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
/// Exception thrown for WebSocket transport errors, such as connection failures,
/// unexpected disconnections, or protocol violations.
/// Optionally captures the WebSocket close status code for diagnostic purposes.
/// Corresponds to Java: io.agentscope.core.model.transport.websocket.WebSocketTransportException
/// WebSocket 传输异常，用于连接失败、意外断开或协议违规等错误。
/// 可选地捕获 WebSocket 关闭状态码以用于诊断。
/// 对应 Java: io.agentscope.core.model.transport.websocket.WebSocketTransportException
/// </summary>
public class WebSocketTransportException : System.Exception
{
    /// <summary>
    /// Gets the WebSocket close status code associated with the error, if available.
    /// This corresponds to WebSocketCloseStatus enum values (e.g., 1000 = NormalClosure, 1006 = Aborted).
    /// 获取与错误关联的 WebSocket 关闭状态码（若可用）。
    /// 对应 WebSocketCloseStatus 枚举值（例如 1000 = NormalClosure，1006 = Aborted）。
    /// </summary>
    public int? CloseStatus { get; }

    /// <summary>
    /// Creates a new WebSocketTransportException with the specified error message.
    /// 使用指定的错误消息创建新的 WebSocketTransportException。
    /// </summary>
    /// <param name="message">Error message describing the WebSocket transport failure / 描述 WebSocket 传输失败的错误消息</param>
    public WebSocketTransportException(string message) : base(message) { }

    /// <summary>
    /// Creates a new WebSocketTransportException with the specified error message
    /// and a reference to the inner exception that caused this error.
    /// 使用指定的错误消息和对导致此错误的内部异常的引用来创建新的 WebSocketTransportException。
    /// </summary>
    /// <param name="message">Error message describing the WebSocket transport failure / 描述 WebSocket 传输失败的错误消息</param>
    /// <param name="inner">The inner exception that caused this error / 导致此错误的内部异常</param>
    public WebSocketTransportException(string message, System.Exception inner) : base(message, inner) { }

    /// <summary>
    /// Creates a new WebSocketTransportException with the specified error message
    /// and WebSocket close status code.
    /// 使用指定的错误消息和 WebSocket 关闭状态码创建新的 WebSocketTransportException。
    /// </summary>
    /// <param name="message">Error message describing the WebSocket transport failure / 描述 WebSocket 传输失败的错误消息</param>
    /// <param name="closeStatus">The WebSocket close status code (e.g., 1000-1015) / WebSocket 关闭状态码（例如 1000-1015）</param>
    public WebSocketTransportException(string message, int closeStatus) : base(message)
    {
        CloseStatus = closeStatus;
    }
}
