// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Net.Http;

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP 客户端构建器，链式构建模式。
/// 对标 Java McpClientBuilder。
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

    public static McpClientBuilder Create() => new();

    /// <summary>设置客户端名称。</summary>
    public McpClientBuilder Named(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>使用 Stdio 传输方式。</summary>
    public McpClientBuilder UseStdio(string command, string? args = null)
    {
        _transportKind = TransportKind.Stdio;
        _command = command ?? throw new ArgumentNullException(nameof(command));
        _arguments = args;
        return this;
    }

    /// <summary>使用 Streamable HTTP 传输方式。</summary>
    public McpClientBuilder UseStreamableHttp(string url)
    {
        _transportKind = TransportKind.StreamableHttp;
        _url = url ?? throw new ArgumentNullException(nameof(url));
        return this;
    }

    /// <summary>使用 SSE 传输方式。</summary>
    public McpClientBuilder UseSse(string url)
    {
        _transportKind = TransportKind.Sse;
        _url = url ?? throw new ArgumentNullException(nameof(url));
        return this;
    }

    /// <summary>设置 API 密钥。</summary>
    public McpClientBuilder WithApiKey(string apiKey)
    {
        _apiKey = apiKey;
        return this;
    }

    /// <summary>设置工作目录（仅对 Stdio 传输有效）。</summary>
    public McpClientBuilder WithWorkingDirectory(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
        return this;
    }

    /// <summary>设置自定义 HttpClient（仅对 HTTP/SSE 传输有效）。</summary>
    public McpClientBuilder WithHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        return this;
    }

    /// <summary>设置请求超时。</summary>
    public McpClientBuilder WithRequestTimeout(TimeSpan timeout)
    {
        _requestTimeout = timeout;
        return this;
    }

    /// <summary>
    /// 构建并返回 IMcpClient 实例。
    /// </summary>
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
                _command ?? throw new InvalidOperationException("Stdio 传输需要指定命令"),
                _arguments,
                _workingDirectory,
                requestTimeout: _requestTimeout),

            TransportKind.StreamableHttp => new StreamableHttpMcpClient(
                name,
                _url ?? throw new InvalidOperationException("Streamable HTTP 传输需要指定 URL"),
                _httpClient,
                _requestTimeout),

            TransportKind.Sse => new SseMcpClient(
                name,
                _url ?? throw new InvalidOperationException("SSE 传输需要指定 URL"),
                _httpClient,
                _apiKey,
                _requestTimeout),

            _ => throw new InvalidOperationException($"不支持的传输方式: {_transportKind}")
        };
    }

    private enum TransportKind
    {
        Stdio,
        StreamableHttp,
        Sse
    }
}
