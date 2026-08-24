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

using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using AgentScope.Core.Model.Transport.WebSocket;
using CoreVersion = AgentScope.Core.Version;

namespace WebSocketEcho;

/// <summary>
/// WebSocket 本地回环示例程序 - 展示如何使用 ClientWebSocketTransport 进行 WebSocket 通信
/// WebSocket local loopback example - demonstrates WebSocket communication using ClientWebSocketTransport
/// </summary>
internal static class Program
{
    /// <summary>
    /// 应用程序入口点 - 建立 WebSocket 连接、发送消息并接收回显
    /// Application entry point - establishes WebSocket connection, sends message and receives echo
    /// </summary>
    private static async Task Main()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var server = LoopbackWebSocketEchoServer.Start();

        Console.WriteLine($"{CoreVersion.GetFullVersion()}\n");
        Console.WriteLine("WebSocket 本地回环示例\n");
        Console.WriteLine($"本地服务地址: {server.Uri}");

        var transport = new ClientWebSocketTransport();
        using var connection = await transport.ConnectAsync(
            new WebSocketRequest { Uri = server.Uri },
            cts.Token);

        Console.WriteLine("连接已建立");

        const string outboundMessage = "hello websocket";
        Console.WriteLine($"发送: {outboundMessage}");
        await connection.SendAsync(outboundMessage, cts.Token);

        await foreach (var inboundMessage in connection.ReceiveAsync(cts.Token))
        {
            Console.WriteLine($"接收: {inboundMessage}");
            break;
        }

        await connection.CloseAsync(cts.Token);
        await server.Completion.WaitAsync(cts.Token);

        Console.WriteLine("连接已关闭");
    }
}

/// <summary>
/// 本地回环 WebSocket 回声服务器 - 接收客户端消息并返回带 "echo:" 前缀的响应
/// Local loopback WebSocket echo server - receives client messages and returns responses with "echo:" prefix
/// </summary>
internal sealed class LoopbackWebSocketEchoServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _serverTask;
    private bool _disposed;

    /// <summary>
    /// 初始化本地回环 WebSocket 回声服务器
    /// Initializes a local loopback WebSocket echo server
    /// </summary>
    /// <param name="listener">HTTP 监听器 / HTTP listener</param>
    /// <param name="cancellationTokenSource">取消令牌源 / Cancellation token source</param>
    /// <param name="serverTask">服务器后台任务 / Server background task</param>
    /// <param name="uri">服务器 URI / Server URI</param>
    private LoopbackWebSocketEchoServer(HttpListener listener, CancellationTokenSource cancellationTokenSource, Task serverTask, Uri uri)
    {
        _listener = listener;
        _cancellationTokenSource = cancellationTokenSource;
        _serverTask = serverTask;
        Uri = uri;
    }

    /// <summary>
    /// 获取 WebSocket 服务器 URI
    /// Gets the WebSocket server URI
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// 获取服务器完成任务 - 等待服务器运行结束
    /// Gets the server completion task - waits for the server to finish running
    /// </summary>
    public Task Completion => _serverTask;

    /// <summary>
    /// 启动本地回环 WebSocket 回声服务器
    /// Starts a local loopback WebSocket echo server
    /// </summary>
    /// <returns>已启动的服务器实例 / The started server instance</returns>
    public static LoopbackWebSocketEchoServer Start()
    {
        var port = GetFreePort();
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/ws/");
        listener.Start();

        var cancellationTokenSource = new CancellationTokenSource();
        var serverTask = Task.Run(() => RunAsync(listener, cancellationTokenSource.Token));

        return new LoopbackWebSocketEchoServer(
            listener,
            cancellationTokenSource,
            serverTask,
            new Uri($"ws://127.0.0.1:{port}/ws/"));
    }

    /// <summary>
    /// 释放服务器资源，取消后台任务并关闭 HTTP 监听器
    /// Disposes server resources, cancels the background task and closes the HTTP listener
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
    /// 运行 WebSocket 服务器 - 接受连接、接收消息并返回回声响应
    /// Runs the WebSocket server - accepts connections, receives messages and returns echo responses
    /// </summary>
    /// <param name="listener">HTTP 监听器 / HTTP listener</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
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

            var inboundMessage = await ReceiveTextAsync(socket, cancellationToken);
            if (inboundMessage is not null)
            {
                var payload = Encoding.UTF8.GetBytes($"echo:{inboundMessage}");
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
    /// 从 WebSocket 接收完整的文本消息（支持分片）
    /// Receives a complete text message from the WebSocket (supports fragmentation)
    /// </summary>
    /// <param name="socket">WebSocket 实例 / WebSocket instance</param>
    /// <param name="cancellationToken">取消令牌 / Cancellation token</param>
    /// <returns>接收到的文本消息，连接关闭时返回 null / The received text message, or null if connection closed</returns>
    private static async Task<string?> ReceiveTextAsync(WebSocket socket, CancellationToken cancellationToken)
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
    /// 获取系统分配的可用空闲端口
    /// Gets an available free port assigned by the system
    /// </summary>
    /// <returns>可用端口号 / Available port number</returns>
    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
