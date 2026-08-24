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
/// Represents an active WebSocket connection for bidirectional communication.
/// Provides methods for receiving, sending, and closing the connection.
/// Corresponds to Java: io.agentscope.core.model.transport.websocket.WebSocketConnection
/// 表示一个活跃的 WebSocket 连接，用于双向通信。
/// 提供接收、发送和关闭连接的方法。
/// 对应 Java: io.agentscope.core.model.transport.websocket.WebSocketConnection
/// </summary>
public interface IWebSocketConnection : IDisposable
{
    /// <summary>
    /// Gets whether the WebSocket connection is currently open.
    /// 获取 WebSocket 连接当前是否处于打开状态。
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Receives text messages as an async enumerable stream.
    /// Each yielded string is a complete text message from the server.
    /// 以异步可枚举流的形式接收文本消息。
    /// 每个生成的字符串都是来自服务器的完整文本消息。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    /// <returns>An async enumerable of received text messages / 接收到的文本消息的异步可枚举流</returns>
    IAsyncEnumerable<string> ReceiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a text message over the WebSocket connection.
    /// 通过 WebSocket 连接发送文本消息。
    /// </summary>
    /// <param name="message">The text message to send / 要发送的文本消息</param>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    /// <returns>A task representing the send operation / 表示发送操作的任务</returns>
    Task SendAsync(string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the WebSocket connection gracefully with a normal closure status.
    /// 以正常关闭状态优雅地关闭 WebSocket 连接。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    /// <returns>A task representing the close operation / 表示关闭操作的任务</returns>
    Task CloseAsync(CancellationToken cancellationToken = default);
}
