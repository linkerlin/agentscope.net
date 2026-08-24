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

using System.Net.WebSockets;
using System.Text;

namespace AgentScope.Core.Model.Transport.WebSocket;

/// <summary>
/// WebSocket transport implementation based on System.Net.WebSockets.ClientWebSocket.
/// Provides connection, message sending/receiving, and graceful close functionality.
/// Corresponds to Java: io.agentscope.core.model.transport.websocket.ClientWebSocketTransport
/// 基于 System.Net.WebSockets.ClientWebSocket 的 WebSocket 传输实现。
/// 提供连接、消息发送/接收和优雅关闭功能。
/// 对应 Java: io.agentscope.core.model.transport.websocket.ClientWebSocketTransport
/// </summary>
public class ClientWebSocketTransport : IWebSocketTransport
{
    /// <summary>
    /// Connects to a WebSocket server using the specified request parameters.
    /// Sets up custom headers and sub-protocol before connecting.
    /// 使用指定的请求参数连接到 WebSocket 服务器。
    /// 在连接前设置自定义请求头和子协议。
    /// </summary>
    /// <param name="request">The WebSocket connection request containing URI, headers, and sub-protocol / 包含 URI、请求头和子协议的 WebSocket 连接请求</param>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    /// <returns>A task representing the connection, with an IWebSocketConnection result / 表示连接的任务，返回 IWebSocketConnection</returns>
    public async Task<IWebSocketConnection> ConnectAsync(WebSocketRequest request, CancellationToken cancellationToken = default)
    {
        if (request?.Uri == null)
            throw new ArgumentNullException(nameof(request));
        var ws = new ClientWebSocket();
        if (request.Headers != null)
        {
            foreach (var (k, v) in request.Headers)
                ws.Options.SetRequestHeader(k, v);
        }
        if (!string.IsNullOrEmpty(request.SubProtocol))
            ws.Options.AddSubProtocol(request.SubProtocol);
        await ws.ConnectAsync(request.Uri, cancellationToken).ConfigureAwait(false);
        return new WebSocketConnection(ws);
    }
}

/// <summary>
/// Wraps a ClientWebSocket instance to implement the IWebSocketConnection interface.
/// Handles message fragmentation by concatenating partial frames into complete messages.
/// 包装 ClientWebSocket 实例以实现 IWebSocketConnection 接口。
/// 通过拼接部分帧为完整消息来处理消息分片。
/// </summary>
public sealed class WebSocketConnection : IWebSocketConnection
{
    private readonly ClientWebSocket _ws;
    private bool _disposed;

    /// <summary>
    /// Initializes a new WebSocketConnection wrapping the specified ClientWebSocket.
    /// 初始化包装指定 ClientWebSocket 的新 WebSocketConnection。
    /// </summary>
    /// <param name="ws">The ClientWebSocket instance to wrap / 要包装的 ClientWebSocket 实例</param>
    public WebSocketConnection(ClientWebSocket ws)
    {
        _ws = ws ?? throw new ArgumentNullException(nameof(ws));
    }

    /// <summary>
    /// Gets whether the underlying WebSocket connection is currently open.
    /// 获取底层 WebSocket 连接当前是否处于打开状态。
    /// </summary>
    public bool IsOpen => _ws.State == WebSocketState.Open;

    /// <summary>
    /// Receives text messages as an async enumerable stream.
    /// Handles message fragmentation by buffering partial frames and yielding complete messages.
    /// 以异步可枚举流的形式接收文本消息。
    /// 通过缓冲部分帧并生成完整消息来处理消息分片。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    /// <returns>An async enumerable of received text messages / 接收到的文本消息的异步可枚举流</returns>
    public async IAsyncEnumerable<string> ReceiveAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = new byte[4096];
        while (_ws.State == WebSocketState.Open)
        {
            var segment = new ArraySegment<byte>(buffer);
            var result = await _ws.ReceiveAsync(segment, cancellationToken).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
                break;
            var text = Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count));
            if (result.EndOfMessage)
                yield return text;
            else
            {
                // Buffer partial frames until the complete message is received
                // 缓冲部分帧，直到收到完整消息
                var sb = new StringBuilder(text);
                while (!result.EndOfMessage)
                {
                    result = await _ws.ReceiveAsync(segment, cancellationToken).ConfigureAwait(false);
                    sb.Append(Encoding.UTF8.GetString(buffer.AsSpan(0, result.Count)));
                }
                yield return sb.ToString();
            }
        }
    }

    /// <summary>
    /// Sends a text message over the WebSocket connection.
    /// Throws InvalidOperationException if the WebSocket is not open.
    /// 通过 WebSocket 连接发送文本消息。
    /// 如果 WebSocket 未打开则抛出 InvalidOperationException。
    /// </summary>
    /// <param name="message">The text message to send / 要发送的文本消息</param>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    /// <returns>A task representing the send operation / 表示发送操作的任务</returns>
    public Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_ws.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket is not open. / WebSocket 未打开。");
        var bytes = Encoding.UTF8.GetBytes(message);
        return _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    /// <summary>
    /// Closes the WebSocket connection gracefully with a normal closure status.
    /// 以正常关闭状态优雅地关闭 WebSocket 连接。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    /// <returns>A task representing the close operation / 表示关闭操作的任务</returns>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_ws.State == WebSocketState.Open)
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the underlying ClientWebSocket resources.
    /// 释放底层 ClientWebSocket 资源。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _ws.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
