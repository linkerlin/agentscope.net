using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using AgentScope.Core.Model.Transport.WebSocket;
using CoreVersion = AgentScope.Core.Version;

namespace WebSocketEcho;

internal static class Program
{
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

internal sealed class LoopbackWebSocketEchoServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _serverTask;
    private bool _disposed;

    private LoopbackWebSocketEchoServer(HttpListener listener, CancellationTokenSource cancellationTokenSource, Task serverTask, Uri uri)
    {
        _listener = listener;
        _cancellationTokenSource = cancellationTokenSource;
        _serverTask = serverTask;
        Uri = uri;
    }

    public Uri Uri { get; }

    public Task Completion => _serverTask;

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

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
