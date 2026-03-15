// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Model.Transport.WebSocket;
using Xunit;

namespace AgentScope.Core.Tests.Model.Transport.WebSocket;

public class WebSocketTransportTests
{
    [Fact]
    public void WebSocketRequest_Uri_CanBeSet()
    {
        var req = new WebSocketRequest { Uri = new Uri("wss://example.com/ws") };
        Assert.NotNull(req.Uri);
        Assert.Equal("wss://example.com/ws", req.Uri.ToString());
    }

    [Fact]
    public async Task ClientWebSocketTransport_ConnectAsync_NullRequest_Throws()
    {
        var transport = new ClientWebSocketTransport();
        await Assert.ThrowsAsync<ArgumentNullException>(() => transport.ConnectAsync(null!));
    }

    [Fact]
    public async Task ClientWebSocketTransport_ConnectAsync_NullUri_Throws()
    {
        var transport = new ClientWebSocketTransport();
        var req = new WebSocketRequest();
        await Assert.ThrowsAsync<ArgumentNullException>(() => transport.ConnectAsync(req));
    }
}
