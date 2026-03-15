// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Model.Transport.WebSocket;

/// <summary>
/// WebSocket 传输层接口，用于实时 TTS、流式模型响应等。
/// </summary>
public interface IWebSocketTransport
{
    Task<IWebSocketConnection> ConnectAsync(WebSocketRequest request, CancellationToken cancellationToken = default);
}
