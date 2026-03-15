// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Model.Transport.WebSocket;

/// <summary>
/// WebSocket 连接：接收、发送、关闭。
/// </summary>
public interface IWebSocketConnection : IDisposable
{
    bool IsOpen { get; }
    IAsyncEnumerable<string> ReceiveAsync(CancellationToken cancellationToken = default);
    Task SendAsync(string message, CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
}
