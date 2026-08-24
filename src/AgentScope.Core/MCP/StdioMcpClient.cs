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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP client implementation based on stdio (standard input/output) transport.
/// Communicates with an MCP server process via its stdin/stdout streams using JSON-RPC.
/// Currently covers the three main workflows: initialize, tools/list, tools/call.
/// Corresponds to Java: io.agentscope.core.mcp.StdioMcpClient
/// 通过标准输入/输出（stdio）与 MCP 服务器通信的客户端实现。
/// 通过子进程的标准输入输出流进行 JSON-RPC 通信。
/// 当前实现覆盖 initialize、tools/list、tools/call 三条主链路。
/// 对应 Java: io.agentscope.core.mcp.StdioMcpClient
/// </summary>
public sealed class StdioMcpClient : McpClientWrapper
{
    /// <summary>Client instance name / 客户端实例名称</summary>
    private readonly string _name;

    /// <summary>Executable file path (e.g., node, python) / 可执行文件路径（如 node、python）</summary>
    private readonly string _fileName;

    /// <summary>Command-line arguments for the executable / 可执行文件的命令行参数</summary>
    private readonly string _arguments;

    /// <summary>Optional working directory for the child process / 子进程的可选工作目录</summary>
    private readonly string? _workingDirectory;

    /// <summary>Environment variables to pass to the child process / 传递给子进程的环境变量</summary>
    private readonly IReadOnlyDictionary<string, string> _environmentVariables;

    /// <summary>Request timeout duration / 请求超时时间</summary>
    private readonly TimeSpan _requestTimeout;

    /// <summary>Semaphore to guard initialization lifecycle / 保护初始化生命周期的信号量</summary>
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    /// <summary>Buffer for collecting stderr output for diagnostics / 收集标准错误输出的诊断缓冲区</summary>
    private readonly StringBuilder _stderrBuffer = new();

    /// <summary>The child process running the MCP server / 运行 MCP 服务器的子进程</summary>
    private Process? _process;

    /// <summary>JSON-RPC connection over the process stdio streams / 基于进程 stdio 流的 JSON-RPC 连接</summary>
    private McpJsonRpcConnection? _connection;

    /// <summary>Background task that reads stderr output / 读取标准错误输出的后台任务</summary>
    private Task? _stderrPumpTask;

    /// <summary>
    /// Initializes a new instance of <see cref="StdioMcpClient"/>.
    /// 初始化 StdioMcpClient 的新实例。
    /// </summary>
    /// <param name="name">Client instance name / 客户端实例名称</param>
    /// <param name="fileName">Executable file path (e.g., node, python) / 可执行文件路径（如 node、python）</param>
    /// <param name="arguments">Command-line arguments (optional) / 命令行参数（可选）</param>
    /// <param name="workingDirectory">Working directory for the child process (optional) / 子进程工作目录（可选）</param>
    /// <param name="environmentVariables">Environment variables for the child process (optional) / 子进程环境变量（可选）</param>
    /// <param name="requestTimeout">Request timeout (optional, default 30s) / 请求超时（可选，默认 30s）</param>
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

    /// <summary>Client instance name / 客户端实例名称</summary>
    public override string Name => _name;

    /// <summary>
    /// Initializes the MCP session by starting the child process and performing the JSON-RPC handshake.
    /// Sends "initialize" request and "notifications/initialized" notification.
    /// 通过启动子进程并执行 JSON-RPC 握手来初始化 MCP 会话。
    /// 发送 "initialize" 请求和 "notifications/initialized" 通知。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
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

    /// <summary>
    /// Lists available tools from the MCP server via JSON-RPC.
    /// 通过 JSON-RPC 从 MCP 服务器列出可用工具。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>List of tool schemas / 工具模式列表</returns>
    public override async Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var result = await ExecuteWithTimeoutAsync(
            token => _connection!.SendRequestAsync("tools/list", new Dictionary<string, object>(), token),
            cancellationToken).ConfigureAwait(false);

        return ParseTools(result);
    }

    /// <summary>
    /// Calls a remote tool via the MCP server using JSON-RPC.
    /// 通过 JSON-RPC 调用 MCP 服务器上的远程工具。
    /// </summary>
    /// <param name="toolName">Tool name / 工具名称</param>
    /// <param name="args">Tool arguments / 工具参数</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>Tool call result / 工具调用结果</returns>
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

    /// <summary>
    /// Disposes the session and releases all resources.
    /// 释放会话和所有资源。
    /// </summary>
    public override void Dispose()
    {
        DisposeSession();
        _lifecycleLock.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Ensures the client is initialized before proceeding; auto-initializes if needed.
    /// 确保客户端已初始化，必要时自动初始化。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (!IsInitialized)
        {
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes an async operation with a timeout, wrapping timeout errors with diagnostic info.
    /// 执行带超时的异步操作，将超时错误包装为包含诊断信息的异常。
    /// </summary>
    /// <typeparam name="T">Return type / 返回类型</typeparam>
    /// <param name="action">Async action to execute / 要执行的异步操作</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>Action result / 操作结果</returns>
    /// <exception cref="TimeoutException">Thrown when the operation times out / 操作超时时抛出</exception>
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

    /// <summary>
    /// Executes an async void operation with a timeout, wrapping timeout errors with diagnostic info.
    /// 执行带超时的异步无返回值操作，将超时错误包装为包含诊断信息的异常。
    /// </summary>
    /// <param name="action">Async action to execute / 要执行的异步操作</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <exception cref="TimeoutException">Thrown when the operation times out / 操作超时时抛出</exception>
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

    /// <summary>
    /// Starts the child process with the configured executable, arguments, and environment.
    /// Also starts a background task to capture stderr output for diagnostics.
    /// 启动配置了可执行文件、参数和环境的子进程。
    /// 同时启动后台任务捕获标准错误输出用于诊断。
    /// </summary>
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
                // Ignore cleanup exceptions during stderr reading / 忽略 stderr 读取阶段的清理异常
            }
        });
    }

    /// <summary>
    /// Creates the JSON-RPC initialize parameters with protocol version and client info.
    /// 创建包含协议版本和客户端信息的 JSON-RPC 初始化参数。
    /// </summary>
    /// <returns>Initialize parameters dictionary / 初始化参数字典</returns>
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

    /// <summary>
    /// Builds a diagnostic string containing process status and stderr output.
    /// 构建包含进程状态和标准错误输出的诊断字符串。
    /// </summary>
    /// <returns>Diagnostic information / 诊断信息</returns>
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

    /// <summary>
    /// Disposes the current session: closes the connection, kills the process, and cleans up the stderr pump.
    /// 释放当前会话：关闭连接、终止进程并清理标准错误输出泵。
    /// </summary>
    private void DisposeSession()
    {
        IsInitialized = false;

        try
        {
            _connection?.Dispose();
        }
        catch
        {
            // Ignore cleanup exceptions during disposal / 忽略释放阶段异常
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
                // Ignore process cleanup exceptions / 忽略进程清理异常
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
            // Ignore background task cleanup exceptions / 忽略后台清理异常
        }
        finally
        {
            _stderrPumpTask = null;
        }
    }

    /// <summary>
    /// Parses the JSON-RPC "tools/list" result into a list of McpToolSchema.
    /// 将 JSON-RPC "tools/list" 结果解析为 McpToolSchema 列表。
    /// </summary>
    /// <param name="result">JSON element containing the tools array / 包含工具数组的 JSON 元素</param>
    /// <returns>List of tool schemas / 工具模式列表</returns>
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

    /// <summary>
    /// Parses the JSON-RPC "tools/call" result into an McpCallResult.
    /// 将 JSON-RPC "tools/call" 结果解析为 McpCallResult。
    /// </summary>
    /// <param name="result">JSON element containing the call result / 包含调用结果的 JSON 元素</param>
    /// <returns>Parsed tool call result / 解析后的工具调用结果</returns>
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

    /// <summary>
    /// JSON-RPC connection over stdio streams using the MCP message framing protocol (Content-Length headers).
    /// Manages request/response correlation via a concurrent dictionary of pending requests.
    /// Corresponds to Java: io.agentscope.core.mcp.StdioMcpClient.McpJsonRpcConnection
    /// 基于 stdio 流的 JSON-RPC 连接，使用 MCP 消息帧协议（Content-Length 头）。
    /// 通过待处理请求的并发字典管理请求/响应关联。
    /// 对应 Java: io.agentscope.core.mcp.StdioMcpClient.McpJsonRpcConnection
    /// </summary>
    private sealed class McpJsonRpcConnection : IDisposable
    {
        /// <summary>Input stream (stdout of the child process) / 输入流（子进程的标准输出）</summary>
        private readonly Stream _input;

        /// <summary>Output stream (stdin of the child process) / 输出流（子进程的标准输入）</summary>
        private readonly Stream _output;

        /// <summary>Semaphore to serialize write operations / 序列化写操作的信号量</summary>
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        /// <summary>Pending requests keyed by JSON-RPC ID / 按 JSON-RPC ID 索引的待处理请求</summary>
        private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonElement>> _pendingRequests = new();

        /// <summary>Cancellation token source for disposal / 用于释放的取消令牌源</summary>
        private readonly CancellationTokenSource _disposeCts = new();

        /// <summary>Provider of diagnostic information for error messages / 错误消息的诊断信息提供者</summary>
        private readonly Func<string> _diagnosticsProvider;

        /// <summary>Background task running the receive loop / 运行接收循环的后台任务</summary>
        private readonly Task _receiveLoopTask;

        /// <summary>Monotonically increasing request ID / 单调递增的请求 ID</summary>
        private long _nextId;

        /// <summary>
        /// Initializes a new instance of <see cref="McpJsonRpcConnection"/>.
        /// 初始化 McpJsonRpcConnection 的新实例。
        /// </summary>
        /// <param name="input">Input stream (stdout of child process) / 输入流（子进程标准输出）</param>
        /// <param name="output">Output stream (stdin of child process) / 输出流（子进程标准输入）</param>
        /// <param name="diagnosticsProvider">Diagnostic info provider / 诊断信息提供者</param>
        public McpJsonRpcConnection(Stream input, Stream output, Func<string> diagnosticsProvider)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _diagnosticsProvider = diagnosticsProvider ?? throw new ArgumentNullException(nameof(diagnosticsProvider));
            _receiveLoopTask = Task.Run(() => ReceiveLoopAsync(_disposeCts.Token));
        }

        /// <summary>
        /// Sends a JSON-RPC request and waits for the response.
        /// 发送 JSON-RPC 请求并等待响应。
        /// </summary>
        /// <param name="method">JSON-RPC method name / JSON-RPC 方法名</param>
        /// <param name="params">Method parameters / 方法参数</param>
        /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
        /// <returns>Response JSON element / 响应 JSON 元素</returns>
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

        /// <summary>
        /// Sends a JSON-RPC notification (no response expected).
        /// 发送 JSON-RPC 通知（不期望响应）。
        /// </summary>
        /// <param name="method">JSON-RPC method name / JSON-RPC 方法名</param>
        /// <param name="params">Method parameters / 方法参数</param>
        /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
        /// <returns>Task representing the send operation / 表示发送操作的任务</returns>
        public Task SendNotificationAsync(string method, object? @params, CancellationToken cancellationToken)
        {
            return WriteMessageAsync(new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["method"] = method,
                ["params"] = @params
            }, cancellationToken);
        }

        /// <summary>
        /// Disposes the connection, cancelling the receive loop and cleaning up resources.
        /// 释放连接，取消接收循环并清理资源。
        /// </summary>
        public void Dispose()
        {
            _disposeCts.Cancel();
            try
            {
                _receiveLoopTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // Ignore background read cleanup exceptions / 忽略后台读取清理异常
            }

            _writeLock.Dispose();
            _disposeCts.Dispose();
        }

        /// <summary>
        /// Writes a JSON-RPC message to the output stream using the MCP framing protocol.
        /// Format: "Content-Length: {length}\r\n\r\n{body}"
        /// 使用 MCP 帧协议将 JSON-RPC 消息写入输出流。
        /// 格式："Content-Length: {length}\r\n\r\n{body}"
        /// </summary>
        /// <param name="payload">Message payload / 消息负载</param>
        /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
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

        /// <summary>
        /// Background loop that continuously reads framed messages from the input stream
        /// and dispatches responses to the corresponding pending requests.
        /// 后台循环，持续从输入流读取帧消息并将响应分发给对应的待处理请求。
        /// </summary>
        /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
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

        /// <summary>
        /// Fails all pending requests with the given exception.
        /// 使用给定的异常使所有待处理请求失败。
        /// </summary>
        /// <param name="ex">Exception to set on all pending requests / 设置到所有待处理请求的异常</param>
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

        /// <summary>
        /// Parses a JSON-RPC error element into an McpException.
        /// 将 JSON-RPC 错误元素解析为 McpException。
        /// </summary>
        /// <param name="errorElement">JSON error element / JSON 错误元素</param>
        /// <returns>Parsed McpException / 解析后的 McpException</returns>
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

        /// <summary>
        /// Reads a framed message from the stream using the MCP protocol (Content-Length header + JSON body).
        /// 使用 MCP 协议（Content-Length 头 + JSON 体）从流中读取帧消息。
        /// </summary>
        /// <param name="stream">Input stream / 输入流</param>
        /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
        /// <returns>Message bytes, or null if the stream is closed / 消息字节，流关闭时返回 null</returns>
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
