// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AgentScope.Core.MCP;

/// <summary>
/// 基于 SSE (Server-Sent Events) 传输的 MCP 客户端实现。
/// 对标 Java SseMcpClient。
/// 使用 HTTP POST 发送请求，通过 SSE 流接收服务端推送的响应。
/// </summary>
public sealed class SseMcpClient : McpClientWrapper
{
    private readonly string _name;
    private readonly string _endpointUrl;
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly TimeSpan _requestTimeout;
    private long _requestId;

    public override string Name => _name;

    public SseMcpClient(
        string name,
        string endpointUrl,
        HttpClient? http = null,
        string? apiKey = null,
        TimeSpan? requestTimeout = null)
    {
        _name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("名称不能为空", nameof(name))
            : name;
        _endpointUrl = string.IsNullOrWhiteSpace(endpointUrl)
            ? throw new ArgumentException("URL 不能为空", nameof(endpointUrl))
            : endpointUrl;
        _http = http ?? new HttpClient();
        _apiKey = apiKey;
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(30);

        if (_apiKey != null)
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }
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
        {
            return Array.Empty<McpToolSchema>();
        }

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
            return McpCallResult.Fail("无响应");
        }

        if (response.TryGetValue("result", out var resultObj))
        {
            return McpCallResult.Ok(JsonSerializer.Serialize(resultObj));
        }

        if (response.TryGetValue("error", out var errorObj))
        {
            return McpCallResult.Fail(JsonSerializer.Serialize(errorObj));
        }

        return McpCallResult.Fail("未知响应");
    }

    public override void Dispose()
    {
        _http.Dispose();
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

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _endpointUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        try
        {
            using var httpResponse = await _http.SendAsync(httpRequest, cts.Token).ConfigureAwait(false);
            httpResponse.EnsureSuccessStatusCode();

            // 从 SSE 流中读取第一个事件作为响应
            var stream = await httpResponse.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            var reader = new StreamReader(stream);
            string? dataLine = null;
            string? eventType = null;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cts.Token).ConfigureAwait(false);
                if (line == null) break;

                if (line.StartsWith("event:", StringComparison.OrdinalIgnoreCase))
                {
                    eventType = line["event:".Length..].Trim();
                }
                else if (line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    dataLine = line["data:".Length..].Trim();
                }
                else if (string.IsNullOrWhiteSpace(line) && dataLine != null)
                {
                    // 空行表示事件结束
                    break;
                }
            }

            if (dataLine == null)
            {
                return null;
            }

            // 尝试从 data 行解析 JSON-RPC 响应
            var doc = JsonDocument.Parse(dataLine);
            var root = doc.RootElement;

            var result = new Dictionary<string, object>();
            if (root.TryGetProperty("result", out var r))
            {
                result["result"] = r.Clone();
            }
            if (root.TryGetProperty("error", out var e))
            {
                result["error"] = e.Clone();
            }
            return result;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
