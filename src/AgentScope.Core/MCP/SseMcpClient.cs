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
using System.Text;
using System.Text.Json;

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP client implementation based on SSE (Server-Sent Events) transport.
/// Maps to Java SseMcpClient.
/// Uses HTTP POST to send requests and receives server-pushed responses via SSE stream.
/// 基于 SSE (Server-Sent Events) 传输的 MCP 客户端实现。
/// 对标 Java SseMcpClient。
/// 使用 HTTP POST 发送请求，通过 SSE 流接收服务端推送的响应。
/// </summary>
public sealed class SseMcpClient : McpClientWrapper
{
    /// <summary>Client instance name / 客户端实例名称</summary>
    private readonly string _name;

    /// <summary>SSE endpoint URL / SSE 端点 URL</summary>
    private readonly string _endpointUrl;

    /// <summary>HTTP client for communication / 用于通信的 HTTP 客户端</summary>
    private readonly HttpClient _http;

    /// <summary>Optional Bearer API key / 可选的 Bearer API 密钥</summary>
    private readonly string? _apiKey;

    /// <summary>Request timeout / 请求超时时间</summary>
    private readonly TimeSpan _requestTimeout;

    /// <summary>Monotonically increasing request ID / 单调递增的请求 ID</summary>
    private long _requestId;

    /// <summary>Client name / 客户端名称</summary>
    public override string Name => _name;

    /// <summary>
    /// Initializes a new instance of <see cref="SseMcpClient"/>.
    /// 初始化 SseMcpClient 的新实例。
    /// </summary>
    /// <param name="name">Client name / 客户端名称</param>
    /// <param name="endpointUrl">SSE endpoint URL / SSE 端点 URL</param>
    /// <param name="http">HTTP client (optional) / HTTP 客户端（可选）</param>
    /// <param name="apiKey">Bearer API key (optional) / Bearer API 密钥（可选）</param>
    /// <param name="requestTimeout">Request timeout (optional, default 30s) / 请求超时（可选，默认 30s）</param>
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

        // Set Bearer auth header if API key is provided / 如果提供了 API 密钥，设置 Bearer 认证头
        if (_apiKey != null)
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    /// <summary>
    /// Initializes the MCP session by sending an "initialize" request.
    /// 通过发送 "initialize" 请求初始化 MCP 会话。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
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

    /// <summary>
    /// Lists available tools from the MCP server.
    /// 从 MCP 服务器列出可用工具。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>List of tool schemas / 工具模式列表</returns>
    public override async Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendJsonRpcAsync("tools/list", null, cancellationToken).ConfigureAwait(false);

        if (response == null || !response.TryGetValue("result", out var resultObj))
        {
            return Array.Empty<McpToolSchema>();
        }

        // Parse the "tools" array from the result / 从结果中解析 "tools" 数组
        if (resultObj is JsonElement resultEl && resultEl.TryGetProperty("tools", out var toolsEl))
        {
            var tools = JsonSerializer.Deserialize<List<McpToolSchema>>(toolsEl.GetRawText());
            return (IReadOnlyList<McpToolSchema>)(tools ?? new List<McpToolSchema>());
        }

        return Array.Empty<McpToolSchema>();
    }

    /// <summary>
    /// Calls a remote tool via the MCP server.
    /// 通过 MCP 服务器调用远程工具。
    /// </summary>
    /// <param name="toolName">Tool name / 工具名称</param>
    /// <param name="args">Tool arguments / 工具参数</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>Tool call result / 工具调用结果</returns>
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

        // Return result or error based on response / 根据响应返回结果或错误
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

    /// <summary>
    /// Disposes the HTTP client resources.
    /// 释放 HTTP 客户端资源。
    /// </summary>
    public override void Dispose()
    {
        _http.Dispose();
    }

    /// <summary>
    /// Sends a JSON-RPC request via HTTP POST and reads the SSE stream response.
    /// 通过 HTTP POST 发送 JSON-RPC 请求并读取 SSE 流响应。
    /// </summary>
    /// <param name="method">JSON-RPC method name / JSON-RPC 方法名</param>
    /// <param name="parameters">Method parameters / 方法参数</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>Parsed response dictionary, or null on timeout / 解析后的响应字典，超时时返回 null</returns>
    private async Task<Dictionary<string, object>?> SendJsonRpcAsync(
        string method,
        object? parameters,
        CancellationToken cancellationToken)
    {
        // Build JSON-RPC request body / 构建 JSON-RPC 请求体
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

            // Read the first event from the SSE stream as response / 从 SSE 流中读取第一个事件作为响应
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
                    // Empty line marks end of event / 空行表示事件结束
                    break;
                }
            }

            if (dataLine == null)
            {
                return null;
            }

            // Parse JSON-RPC response from the data line / 从 data 行解析 JSON-RPC 响应
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
