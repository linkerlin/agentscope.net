// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Model.Transport.WebSocket;

/// <summary>
/// WebSocket 连接请求参数。
/// </summary>
public class WebSocketRequest
{
    public Uri Uri { get; set; } = null!;
    public Dictionary<string, string>? Headers { get; set; }
    public string? SubProtocol { get; set; }
}
