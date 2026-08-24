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

using System.Net.Http;

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP client builder using a fluent/chained construction pattern.
/// Corresponds to Java: io.agentscope.core.mcp.McpClientBuilder
/// Supports three transport modes: Stdio, Streamable HTTP, and SSE.
/// MCP 客户端构建器，链式构建模式。
/// 对标 Java: io.agentscope.core.mcp.McpClientBuilder
/// 支持 Stdio、Streamable HTTP、SSE 三种传输方式。
/// </summary>
public sealed class McpClientBuilder
{
    private string? _name;
    private TransportKind _transportKind;
    private string? _command;
    private string? _arguments;
    private string? _url;
    private string? _apiKey;
    private string? _workingDirectory;
    private HttpClient? _httpClient;
    private TimeSpan? _requestTimeout;

    private McpClientBuilder()
    {
    }

    /// <summary>
    /// Creates a new McpClientBuilder instance.
    /// 创建一个新的 McpClientBuilder 实例。
    /// </summary>
    /// <returns>A new McpClientBuilder / 一个新的 McpClientBuilder</returns>
    public static McpClientBuilder Create() => new();

    /// <summary>
    /// Sets the client name.
    /// 设置客户端名称。
    /// </summary>
    /// <param name="name">The client name / 客户端名称</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例</returns>
    public McpClientBuilder Named(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Configures the client to use Stdio transport (spawns a subprocess).
    /// 配置客户端使用 Stdio 传输方式（启动子进程）。
    /// </summary>
    /// <param name="command">The command to execute / 要执行的命令</param>
    /// <param name="args">Optional command-line arguments / 可选的命令行参数</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例</returns>
    public McpClientBuilder UseStdio(string command, string? args = null)
    {
        _transportKind = TransportKind.Stdio;
        _command = command ?? throw new ArgumentNullException(nameof(command));
        _arguments = args;
        return this;
    }

    /// <summary>
    /// Configures the client to use Streamable HTTP transport.
    /// 配置客户端使用 Streamable HTTP 传输方式。
    /// </summary>
    /// <param name="url">The server endpoint URL / 服务器端点 URL</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例</returns>
    public McpClientBuilder UseStreamableHttp(string url)
    {
        _transportKind = TransportKind.StreamableHttp;
        _url = url ?? throw new ArgumentNullException(nameof(url));
        return this;
    }

    /// <summary>
    /// Configures the client to use SSE (Server-Sent Events) transport.
    /// 配置客户端使用 SSE（服务器推送事件）传输方式。
    /// </summary>
    /// <param name="url">The SSE endpoint URL / SSE 端点 URL</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例</returns>
    public McpClientBuilder UseSse(string url)
    {
        _transportKind = TransportKind.Sse;
        _url = url ?? throw new ArgumentNullException(nameof(url));
        return this;
    }

    /// <summary>
    /// Sets the API key for authentication (Bearer token).
    /// 设置 API 密钥用于认证（Bearer token）。
    /// </summary>
    /// <param name="apiKey">The API key / API 密钥</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例</returns>
    public McpClientBuilder WithApiKey(string apiKey)
    {
        _apiKey = apiKey;
        return this;
    }

    /// <summary>
    /// Sets the working directory (only effective for Stdio transport).
    /// 设置工作目录（仅对 Stdio 传输有效）。
    /// </summary>
    /// <param name="workingDirectory">The working directory path / 工作目录路径</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例</returns>
    public McpClientBuilder WithWorkingDirectory(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
        return this;
    }

    /// <summary>
    /// Sets a custom HttpClient (only effective for HTTP/SSE transport).
    /// 设置自定义 HttpClient（仅对 HTTP/SSE 传输有效）。
    /// </summary>
    /// <param name="httpClient">The custom HttpClient / 自定义 HttpClient</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例</returns>
    public McpClientBuilder WithHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        return this;
    }

    /// <summary>
    /// Sets the request timeout.
    /// 设置请求超时。
    /// </summary>
    /// <param name="timeout">The timeout duration / 超时时间</param>
    /// <returns>The builder instance for chaining / 用于链式调用的构建器实例</returns>
    public McpClientBuilder WithRequestTimeout(TimeSpan timeout)
    {
        _requestTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Builds and returns the IMcpClient instance based on the configured transport.
    /// 根据配置的传输方式构建并返回 IMcpClient 实例。
    /// </summary>
    /// <returns>An initialized IMcpClient / 一个初始化的 IMcpClient</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required parameters for the selected transport are missing.
    /// 当所选传输方式的必需参数缺失时抛出。
    /// </exception>
    public IMcpClient Build()
    {
        var name = _name ?? $"mcp-{_transportKind}-{Guid.NewGuid():N}";

        if (_apiKey != null)
        {
            _httpClient ??= new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }

        return _transportKind switch
        {
            TransportKind.Stdio => new StdioMcpClient(
                name,
                _command ?? throw new InvalidOperationException("Stdio transport requires a command / Stdio 传输需要指定命令"),
                _arguments,
                _workingDirectory,
                requestTimeout: _requestTimeout),

            TransportKind.StreamableHttp => new StreamableHttpMcpClient(
                name,
                _url ?? throw new InvalidOperationException("Streamable HTTP transport requires a URL / Streamable HTTP 传输需要指定 URL"),
                _httpClient,
                _requestTimeout),

            TransportKind.Sse => new SseMcpClient(
                name,
                _url ?? throw new InvalidOperationException("SSE transport requires a URL / SSE 传输需要指定 URL"),
                _httpClient,
                _apiKey,
                _requestTimeout),

            _ => throw new InvalidOperationException($"Unsupported transport kind: {_transportKind} / 不支持的传输方式: {_transportKind}")
        };
    }

    /// <summary>
    /// Internal enum for MCP transport types.
    /// MCP 传输类型内部枚举。
    /// </summary>
    private enum TransportKind
    {
        Stdio,
        StreamableHttp,
        Sse
    }
}
