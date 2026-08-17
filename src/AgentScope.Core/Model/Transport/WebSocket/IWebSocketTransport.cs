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
/// WebSocket transport layer interface for real-time TTS, streaming model responses, etc.
/// WebSocket 传输层接口，用于实时 TTS、流式模型响应等。
/// </summary>
public interface IWebSocketTransport
{
    /// <summary>
    /// Connects to a WebSocket server using the specified request parameters.
    /// 使用指定的请求参数连接到 WebSocket 服务器。
    /// </summary>
    /// <param name="request">The WebSocket connection request containing URI, headers, etc. / 包含 URI、请求头等的 WebSocket 连接请求</param>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    /// <returns>A task representing the connection, with an <see cref="IWebSocketConnection"/> result / 表示连接的任务，返回 IWebSocketConnection</returns>
    Task<IWebSocketConnection> ConnectAsync(WebSocketRequest request, CancellationToken cancellationToken = default);
}
