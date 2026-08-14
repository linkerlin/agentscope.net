// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Core.MCP;

/// <summary>
/// 通过 HTTP POST + SSE (Streamable HTTP) 与 MCP server 通信的客户端实现。
/// 对应 Java: io.agentscope.core.mcp.StreamableHttpMcpClient
/// </summary>
public sealed class StreamableHttpMcpClient : McpClientWrapper
{
    private readonly string _name;
    private readonly string _baseUrl;
    private readonly HttpClient _http;
    private readonly TimeSpan _requestTimeout;
    private long _requestId;

    public override string Name => _name;

    public StreamableHttpMcpClient(
        string name,
        string baseUrl,
        HttpClient? http = null,
        TimeSpan? requestTimeout = null)
    {
        _name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("名称不能为空", nameof(name))
            : name;
        _baseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? throw new ArgumentException("URL 不能为空", nameof(baseUrl))
            : baseUrl;
        _http = http ?? new HttpClient();
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(30);
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendJsonRpcAsync("initialize", new
        {
            protocolVersion = "2025-03-26",
            capabilities = new { },
            clientInfo = new { name = "AgentScope.NET", version = "1.2.0" }
        }, cancellationToken).ConfigureAwait(false);

        IsInitialized = response != null;
    }

    public override async Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendJsonRpcAsync("tools/list", null, cancellationToken).ConfigureAwait(false);

        if (response == null || !response.TryGetValue("result", out var resultObj))
            return new List<McpToolSchema>();

        if (resultObj is JsonElement resultEl && resultEl.TryGetProperty("tools", out var toolsEl))
        {
            var tools = JsonSerializer.Deserialize<List<McpToolSchema>>(toolsEl.GetRawText());
            return (IReadOnlyList<McpToolSchema>)(tools ?? new List<McpToolSchema>());
        }

        return Array.Empty<McpToolSchema>();
    }

    public override async Task<McpCallResult> CallToolAsync(
        string toolName,
        Dictionary<string, object> args,
        CancellationToken cancellationToken = default)
    {
        var response = await SendJsonRpcAsync("tools/call", new
        {
            name = toolName,
            arguments = args
        }, cancellationToken).ConfigureAwait(false);

        if (response == null)
        {
            return new McpCallResult { IsError = true, Content = "无响应" };
        }

        if (response.TryGetValue("result", out var resultObj))
        {
            return new McpCallResult
            {
                IsError = false,
                Content = JsonSerializer.Serialize(resultObj)
            };
        }

        if (response.TryGetValue("error", out var errorObj))
        {
            return new McpCallResult
            {
                IsError = true,
                Content = JsonSerializer.Serialize(errorObj)
            };
        }

        return new McpCallResult { IsError = true, Content = "未知响应" };
    }

    private async Task<Dictionary<string, object>?> SendJsonRpcAsync(
        string method,
        object? parameters,
        CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _requestId);
        var requestBody = new Dictionary<string, object>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method
        };
        if (parameters != null)
        {
            requestBody["params"] = parameters;
        }

        var json = JsonSerializer.Serialize(requestBody);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_requestTimeout);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        try
        {
            using var httpResponse = await _http.SendAsync(httpRequest, cts.Token).ConfigureAwait(false);
            httpResponse.EnsureSuccessStatusCode();

            var body = await httpResponse.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

            // 尝试解析为 JSON-RPC 响应
            var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var result = new Dictionary<string, object?>
            {
                ["result"] = root.TryGetProperty("result", out var r) ? r : null,
                ["error"] = root.TryGetProperty("error", out var e) ? e : null
            };

            var dict = new Dictionary<string, object>();
            foreach (var kv in result)
            {
                if (kv.Value != null)
                    dict[kv.Key] = kv.Value;
            }
            return dict;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public override void Dispose()
    {
        _http.Dispose();
    }
}
