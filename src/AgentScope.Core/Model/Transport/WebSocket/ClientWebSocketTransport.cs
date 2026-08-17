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
/// 基于 System.Net.WebSockets.ClientWebSocket 的 WebSocket 传输实现。
/// </summary>
public class ClientWebSocketTransport : IWebSocketTransport
{
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
/// 包装 ClientWebSocket，实现 IWebSocketConnection。
/// </summary>
public sealed class WebSocketConnection : IWebSocketConnection
{
    private readonly ClientWebSocket _ws;
    private bool _disposed;

    public WebSocketConnection(ClientWebSocket ws)
    {
        _ws = ws ?? throw new ArgumentNullException(nameof(ws));
    }

    public bool IsOpen => _ws.State == WebSocketState.Open;

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

    public Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_ws.State != WebSocketState.Open)
            throw new InvalidOperationException("WebSocket 未打开。");
        var bytes = Encoding.UTF8.GetBytes(message);
        return _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_ws.State == WebSocketState.Open)
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _ws.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
