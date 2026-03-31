// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Model.Transport.WebSocket;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using SystemWebSocket = System.Net.WebSockets.WebSocket;
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

    [Fact]
    public async Task ClientWebSocketTransport_WithLoopbackServer_CanRoundTripMessage()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var server = LoopbackWebSocketServer.Start();
        var transport = new ClientWebSocketTransport();

        using var connection = await transport.ConnectAsync(
            new WebSocketRequest { Uri = server.Uri },
            cts.Token);

        Assert.True(connection.IsOpen);

        await connection.SendAsync("ping", cts.Token);

        await using var enumerator = connection.ReceiveAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal("echo:ping", enumerator.Current);

        await connection.CloseAsync(cts.Token);
        await server.Completion.WaitAsync(cts.Token);

        Assert.False(connection.IsOpen);
    }

    private sealed class LoopbackWebSocketServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _serverTask;
        private bool _disposed;

        private LoopbackWebSocketServer(HttpListener listener, CancellationTokenSource cancellationTokenSource, Task serverTask, Uri uri)
        {
            _listener = listener;
            _cancellationTokenSource = cancellationTokenSource;
            _serverTask = serverTask;
            Uri = uri;
        }

        public Uri Uri { get; }

        public Task Completion => _serverTask;

        public static LoopbackWebSocketServer Start()
        {
            var port = GetFreePort();
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/ws/");
            listener.Start();

            var cancellationTokenSource = new CancellationTokenSource();
            var serverTask = Task.Run(() => RunAsync(listener, cancellationTokenSource.Token));

            return new LoopbackWebSocketServer(
                listener,
                cancellationTokenSource,
                serverTask,
                new Uri($"ws://127.0.0.1:{port}/ws/"));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _cancellationTokenSource.Cancel();
            _listener.Close();

            try
            {
                _serverTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
            }

            _cancellationTokenSource.Dispose();
            _disposed = true;
        }

        private static async Task RunAsync(HttpListener listener, CancellationToken cancellationToken)
        {
            try
            {
                var context = await listener.GetContextAsync().WaitAsync(cancellationToken);
                if (!context.Request.IsWebSocketRequest)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.Close();
                    return;
                }

                var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                using var socket = webSocketContext.WebSocket;

                var message = await ReceiveTextAsync(socket, cancellationToken);
                if (message is not null)
                {
                    var payload = Encoding.UTF8.GetBytes($"echo:{message}");
                    await socket.SendAsync(
                        new ArraySegment<byte>(payload),
                        WebSocketMessageType.Text,
                        true,
                        cancellationToken);
                }

                var buffer = new byte[256];
                while (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", cancellationToken);
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        private static async Task<string?> ReceiveTextAsync(SystemWebSocket socket, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024];
            var builder = new StringBuilder();

            while (true)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return null;
                }

                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (result.EndOfMessage)
                {
                    return builder.ToString();
                }
            }
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
