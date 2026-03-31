// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AgentScope.Core.MCP;

/// <summary>
/// 通过 stdio 与 MCP server 通信的客户端实现。
/// 当前实现覆盖 initialize、tools/list、tools/call 三条主链路。
/// </summary>
public sealed class StdioMcpClient : McpClientWrapper
{
    private readonly string _name;
    private readonly string _fileName;
    private readonly string _arguments;
    private readonly string? _workingDirectory;
    private readonly IReadOnlyDictionary<string, string> _environmentVariables;
    private readonly TimeSpan _requestTimeout;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private readonly StringBuilder _stderrBuffer = new();

    private Process? _process;
    private McpJsonRpcConnection? _connection;
    private Task? _stderrPumpTask;

    public StdioMcpClient(
        string name,
        string fileName,
        string? arguments = null,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        TimeSpan? requestTimeout = null)
    {
        _name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("名称不能为空", nameof(name)) : name;
        _fileName = string.IsNullOrWhiteSpace(fileName) ? throw new ArgumentException("命令不能为空", nameof(fileName)) : fileName;
        _arguments = arguments ?? string.Empty;
        _workingDirectory = workingDirectory;
        _environmentVariables = environmentVariables ?? new Dictionary<string, string>();
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(30);
    }

    public override string Name => _name;

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            return;
        }

        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsInitialized)
            {
                return;
            }

            StartProcess();
            _connection = new McpJsonRpcConnection(
                _process!.StandardOutput.BaseStream,
                _process.StandardInput.BaseStream,
                BuildDiagnostics);

            try
            {
                await ExecuteWithTimeoutAsync(
                    token => _connection.SendRequestAsync("initialize", CreateInitializeParams(), token),
                    cancellationToken).ConfigureAwait(false);

                await ExecuteWithTimeoutAsync(
                    token => _connection.SendNotificationAsync("notifications/initialized", new Dictionary<string, object>(), token),
                    cancellationToken).ConfigureAwait(false);

                IsInitialized = true;
            }
            catch
            {
                DisposeSession();
                throw;
            }
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    public override async Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var result = await ExecuteWithTimeoutAsync(
            token => _connection!.SendRequestAsync("tools/list", new Dictionary<string, object>(), token),
            cancellationToken).ConfigureAwait(false);

        return ParseTools(result);
    }

    public override async Task<McpCallResult> CallToolAsync(string toolName, Dictionary<string, object> args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("工具名不能为空", nameof(toolName));
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var payload = new Dictionary<string, object>
        {
            ["name"] = toolName,
            ["arguments"] = args ?? new Dictionary<string, object>()
        };

        var result = await ExecuteWithTimeoutAsync(
            token => _connection!.SendRequestAsync("tools/call", payload, token),
            cancellationToken).ConfigureAwait(false);

        return ParseCallToolResult(result);
    }

    public override void Dispose()
    {
        DisposeSession();
        _lifecycleLock.Dispose();
        base.Dispose();
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!IsInitialized)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(_requestTimeout);

        try
        {
            return await action(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"MCP 请求超时（{_requestTimeout}）: {BuildDiagnostics()}", ex);
        }
    }

    private async Task ExecuteWithTimeoutAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(_requestTimeout);

        try
        {
            await action(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"MCP 请求超时（{_requestTimeout}）: {BuildDiagnostics()}", ex);
        }
    }

    private void StartProcess()
    {
        var psi = new ProcessStartInfo
        {
            FileName = _fileName,
            Arguments = _arguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _workingDirectory ?? Environment.CurrentDirectory
        };

        foreach (var pair in _environmentVariables)
        {
            psi.Environment[pair.Key] = pair.Value;
        }

        _process = new Process { StartInfo = psi };
        _process.Start();
        _stderrPumpTask = Task.Run(async () =>
        {
            try
            {
                var stderr = await _process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                lock (_stderrBuffer)
                {
                    _stderrBuffer.Append(stderr);
                }
            }
            catch
            {
                // 忽略 stderr 读取阶段的清理异常。
            }
        });
    }

    private Dictionary<string, object> CreateInitializeParams()
    {
        return new Dictionary<string, object>
        {
            ["protocolVersion"] = "2024-11-05",
            ["capabilities"] = new Dictionary<string, object>(),
            ["clientInfo"] = new Dictionary<string, object>
            {
                ["name"] = "AgentScope.NET",
                ["version"] = typeof(StdioMcpClient).Assembly.GetName().Version?.ToString() ?? "0.0.0"
            }
        };
    }

    private string BuildDiagnostics()
    {
        var stderr = string.Empty;
        lock (_stderrBuffer)
        {
            stderr = _stderrBuffer.ToString().Trim();
        }

        var exitInfo = _process == null
            ? "进程未启动"
            : _process.HasExited
                ? $"进程已退出，ExitCode={_process.ExitCode}"
                : "进程仍在运行";

        if (string.IsNullOrWhiteSpace(stderr))
        {
            return exitInfo;
        }

        return exitInfo + "；stderr=" + stderr;
    }

    private void DisposeSession()
    {
        IsInitialized = false;

        try
        {
            _connection?.Dispose();
        }
        catch
        {
            // 忽略释放阶段异常。
        }
        finally
        {
            _connection = null;
        }

        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // 忽略进程清理异常。
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }

        try
        {
            _stderrPumpTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // 忽略后台清理异常。
        }
        finally
        {
            _stderrPumpTask = null;
        }
    }

    private static IReadOnlyList<McpToolSchema> ParseTools(JsonElement result)
    {
        if (!result.TryGetProperty("tools", out var toolsElement) || toolsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<McpToolSchema>();
        }

        var tools = new List<McpToolSchema>();
        foreach (var toolElement in toolsElement.EnumerateArray())
        {
            if (!toolElement.TryGetProperty("name", out var nameElement))
            {
                continue;
            }

            tools.Add(new McpToolSchema
            {
                Name = nameElement.GetString() ?? string.Empty,
                Description = toolElement.TryGetProperty("description", out var descriptionElement)
                    ? descriptionElement.GetString()
                    : null,
                InputSchema = toolElement.TryGetProperty("inputSchema", out var inputSchemaElement) && inputSchemaElement.ValueKind != JsonValueKind.Null
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(inputSchemaElement.GetRawText())
                    : null
            });
        }

        return tools;
    }

    private static McpCallResult ParseCallToolResult(JsonElement result)
    {
        var isError = result.TryGetProperty("isError", out var isErrorElement)
            && isErrorElement.ValueKind == JsonValueKind.True;

        var parts = new List<McpContentItem>();
        if (result.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in contentElement.EnumerateArray())
            {
                parts.Add(new McpContentItem
                {
                    Type = item.TryGetProperty("type", out var typeElement)
                        ? typeElement.GetString() ?? "text"
                        : "text",
                    Text = item.TryGetProperty("text", out var textElement)
                        ? textElement.GetString()
                        : null,
                    Data = item.TryGetProperty("data", out var dataElement)
                        ? dataElement.GetString()
                        : null,
                    MimeType = item.TryGetProperty("mimeType", out var mimeTypeElement)
                        ? mimeTypeElement.GetString()
                        : null
                });
            }
        }

        var content = string.Join(
            "\n",
            parts.Where(static part => !string.IsNullOrWhiteSpace(part.Text))
                .Select(static part => part.Text!));

        if (isError)
        {
            return McpCallResult.Fail(content);
        }

        return McpCallResult.Ok(string.IsNullOrWhiteSpace(content) ? null : content, parts);
    }

    private sealed class McpJsonRpcConnection : IDisposable
    {
        private readonly Stream _input;
        private readonly Stream _output;
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonElement>> _pendingRequests = new();
        private readonly CancellationTokenSource _disposeCts = new();
        private readonly Func<string> _diagnosticsProvider;
        private readonly Task _receiveLoopTask;
        private long _nextId;

        public McpJsonRpcConnection(Stream input, Stream output, Func<string> diagnosticsProvider)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _diagnosticsProvider = diagnosticsProvider ?? throw new ArgumentNullException(nameof(diagnosticsProvider));
            _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_disposeCts.Token));
        }

        public async Task<JsonElement> SendRequestAsync(string method, object? @params, CancellationToken cancellationToken)
        {
            var id = Interlocked.Increment(ref _nextId);
            var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRequests[id] = tcs;

            using var registration = cancellationToken.Register(() =>
            {
                if (_pendingRequests.TryRemove(id, out var pending))
                {
                    pending.TrySetCanceled(cancellationToken);
                }
            });

            await WriteMessageAsync(new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["method"] = method,
                ["params"] = @params
            }, cancellationToken).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }

        public Task SendNotificationAsync(string method, object? @params, CancellationToken cancellationToken)
        {
            return WriteMessageAsync(new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["method"] = method,
                ["params"] = @params
            }, cancellationToken);
        }

        public void Dispose()
        {
            _disposeCts.Cancel();
            try
            {
                _receiveLoopTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // 忽略后台读取清理异常。
            }

            _writeLock.Dispose();
            _disposeCts.Dispose();
        }

        private async Task WriteMessageAsync(Dictionary<string, object?> payload, CancellationToken cancellationToken)
        {
            var bodyBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
            var headerBytes = Encoding.ASCII.GetBytes($"Content-Length: {bodyBytes.Length}\r\n\r\n");

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _output.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
                await _output.WriteAsync(bodyBytes, cancellationToken).ConfigureAwait(false);
                await _output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var messageBytes = await ReadFramedMessageAsync(_input, cancellationToken).ConfigureAwait(false);
                    if (messageBytes == null)
                    {
                        FailPendingRequests(new McpException("MCP 连接已关闭: " + _diagnosticsProvider()));
                        break;
                    }

                    using var document = JsonDocument.Parse(messageBytes);
                    var root = document.RootElement;

                    if (!root.TryGetProperty("id", out var idElement) || !idElement.TryGetInt64(out var id))
                    {
                        continue;
                    }

                    if (!_pendingRequests.TryRemove(id, out var pending))
                    {
                        continue;
                    }

                    if (root.TryGetProperty("error", out var errorElement))
                    {
                        pending.TrySetException(ParseError(errorElement));
                        continue;
                    }

                    if (!root.TryGetProperty("result", out var resultElement))
                    {
                        pending.TrySetException(new McpException("MCP 响应缺少 result 字段"));
                        continue;
                    }

                    pending.TrySetResult(resultElement.Clone());
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                FailPendingRequests(new OperationCanceledException("MCP 连接已释放"));
            }
            catch (global::System.Exception ex)
            {
                FailPendingRequests(new McpException("读取 MCP 响应失败: " + _diagnosticsProvider(), ex));
            }
        }

        private void FailPendingRequests(global::System.Exception ex)
        {
            foreach (var pair in _pendingRequests.ToArray())
            {
                if (_pendingRequests.TryRemove(pair.Key, out var pending))
                {
                    pending.TrySetException(ex);
                }
            }
        }

        private static McpException ParseError(JsonElement errorElement)
        {
            var code = errorElement.TryGetProperty("code", out var codeElement) && codeElement.TryGetInt32(out var errorCode)
                ? errorCode
                : (int?)null;
            var message = errorElement.TryGetProperty("message", out var messageElement)
                ? messageElement.GetString()
                : "未知 MCP 错误";

            return new McpException(message ?? "未知 MCP 错误")
            {
                Code = code
            };
        }

        private static async Task<byte[]?> ReadFramedMessageAsync(Stream stream, CancellationToken cancellationToken)
        {
            var headerLines = new List<string>();
            while (true)
            {
                var line = await ReadLineAsync(stream, cancellationToken).ConfigureAwait(false);
                if (line == null)
                {
                    return headerLines.Count == 0 ? null : throw new EndOfStreamException("MCP 消息头不完整");
                }

                if (line.Length == 0)
                {
                    break;
                }

                headerLines.Add(line);
            }

            var contentLength = 0;
            foreach (var headerLine in headerLines)
            {
                var separatorIndex = headerLine.IndexOf(':');
                if (separatorIndex < 0)
                {
                    continue;
                }

                var key = headerLine.Substring(0, separatorIndex).Trim();
                if (!key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = headerLine.Substring(separatorIndex + 1).Trim();
                contentLength = int.Parse(value, CultureInfo.InvariantCulture);
                break;
            }

            if (contentLength <= 0)
            {
                throw new McpException("MCP 消息缺少有效的 Content-Length");
            }

            var body = new byte[contentLength];
            var offset = 0;
            while (offset < contentLength)
            {
                var bytesRead = await stream.ReadAsync(body.AsMemory(offset, contentLength - offset), cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException("MCP 消息体读取未完成");
                }

                offset += bytesRead;
            }

            return body;
        }

        private static async Task<string?> ReadLineAsync(Stream stream, CancellationToken cancellationToken)
        {
            var bytes = new List<byte>();
            var buffer = new byte[1];

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    if (bytes.Count == 0)
                    {
                        return null;
                    }

                    break;
                }

                if (buffer[0] == (byte)'\n')
                {
                    break;
                }

                bytes.Add(buffer[0]);
            }

            var line = Encoding.ASCII.GetString(bytes.ToArray());
            return line.TrimEnd('\r');
        }
    }
}