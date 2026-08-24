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

/// <summary>
/// Tests for WebSocket transport types: <see cref="WebSocketRequest"/>, <see cref="ClientWebSocketTransport"/>, and loopback server.
/// 对 WebSocket 传输类型的测试：WebSocketRequest、ClientWebSocketTransport 及回环服务器。
/// </summary>
public class WebSocketTransportTests
{
    [Fact]
    /// <summary>
    /// Tests that the <see cref="WebSocketRequest.Uri"/> property can be set.
    /// 测试 WebSocketRequest.Uri 属性可以被设置。
    /// </summary>
    public void WebSocketRequest_Uri_CanBeSet()
    {
        var req = new WebSocketRequest { Uri = new Uri("wss://example.com/ws") };
        Assert.NotNull(req.Uri);
        Assert.Equal("wss://example.com/ws", req.Uri.ToString());
    }

    [Fact]
    /// <summary>
    /// Tests that connecting with a null request throws <see cref="ArgumentNullException"/>.
    /// 测试传入 null 请求时 ConnectAsync 抛出 ArgumentNullException。
    /// </summary>
    public async Task ClientWebSocketTransport_ConnectAsync_NullRequest_Throws()
    {
        var transport = new ClientWebSocketTransport();
        await Assert.ThrowsAsync<ArgumentNullException>(() => transport.ConnectAsync(null!));
    }

    [Fact]
    /// <summary>
    /// Tests that connecting with a request containing a null URI throws <see cref="ArgumentNullException"/>.
    /// 测试请求中 URI 为 null 时 ConnectAsync 抛出 ArgumentNullException。
    /// </summary>
    public async Task ClientWebSocketTransport_ConnectAsync_NullUri_Throws()
    {
        var transport = new ClientWebSocketTransport();
        var req = new WebSocketRequest();
        await Assert.ThrowsAsync<ArgumentNullException>(() => transport.ConnectAsync(req));
    }

    [Fact]
    /// <summary>
    /// Tests a full round-trip message exchange with a loopback WebSocket server.
    /// 测试通过回环 WebSocket 服务器完成完整的消息往返交换。
    /// </summary>
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

    /// <summary>
    /// A loopback WebSocket server for testing purposes. Echoes received messages with an "echo:" prefix.
    /// 用于测试的回环 WebSocket 服务器。将收到的消息以 "echo:" 前缀回显。
    /// </summary>
    private sealed class LoopbackWebSocketServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _serverTask;
        private bool _disposed;

        /// <summary>
        /// Initializes the loopback server with the specified dependencies.
        /// 使用指定的依赖项初始化回环服务器。
        /// </summary>
        private LoopbackWebSocketServer(HttpListener listener, CancellationTokenSource cancellationTokenSource, Task serverTask, Uri uri)
        {
            _listener = listener;
            _cancellationTokenSource = cancellationTokenSource;
            _serverTask = serverTask;
            Uri = uri;
        }

        /// <summary>
        /// Gets the WebSocket URI of this server.
        /// 获取此服务器的 WebSocket URI。
        /// </summary>
        public Uri Uri { get; }

        /// <summary>
        /// Gets a task that completes when the server finishes running.
        /// 获取一个在服务器运行完成时完成的任务。
        /// </summary>
        public Task Completion => _serverTask;

        /// <summary>
        /// Starts a new loopback WebSocket server on a random free port.
        /// 在随机空闲端口上启动一个新的回环 WebSocket 服务器。
        /// </summary>
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

        /// <summary>
        /// Disposes the server by canceling the token and closing the listener.
        /// 通过取消令牌并关闭监听器来释放服务器资源。
        /// </summary>
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

        /// <summary>
        /// Runs the WebSocket server loop: accepts one connection, echoes the first message, then waits for close.
        /// 运行 WebSocket 服务器循环：接受一个连接，回显第一条消息，然后等待关闭。
        /// </summary>
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

        /// <summary>
        /// Receives a complete text message from the WebSocket. Returns null if the remote peer sends a close frame.
        /// 从 WebSocket 接收一条完整的文本消息。如果远程端发送了关闭帧则返回 null。
        /// </summary>
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

        /// <summary>
        /// Gets a random free TCP port on the loopback interface.
        /// 在回环接口上获取一个随机的空闲 TCP 端口。
        /// </summary>
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
