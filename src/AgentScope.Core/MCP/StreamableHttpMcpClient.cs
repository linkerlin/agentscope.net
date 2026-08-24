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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP client implementation that communicates with an MCP server via HTTP POST + SSE (Streamable HTTP).
/// Uses JSON-RPC over HTTP with the "text/event-stream" content type for streaming-capable communication.
/// Corresponds to Java: io.agentscope.core.mcp.StreamableHttpMcpClient
/// 通过 HTTP POST + SSE (Streamable HTTP) 与 MCP 服务器通信的客户端实现。
/// 使用基于 HTTP 的 JSON-RPC，配合 "text/event-stream" 内容类型实现流式通信。
/// 对应 Java: io.agentscope.core.mcp.StreamableHttpMcpClient
/// </summary>
public sealed class StreamableHttpMcpClient : McpClientWrapper
{
    /// <summary>Client instance name / 客户端实例名称</summary>
    private readonly string _name;

    /// <summary>Base URL of the MCP server / MCP 服务器的基础 URL</summary>
    private readonly string _baseUrl;

    /// <summary>HTTP client for sending requests / 用于发送请求的 HTTP 客户端</summary>
    private readonly HttpClient _http;

    /// <summary>Request timeout duration / 请求超时时间</summary>
    private readonly TimeSpan _requestTimeout;

    /// <summary>Monotonically increasing JSON-RPC request ID / 单调递增的 JSON-RPC 请求 ID</summary>
    private long _requestId;

    /// <summary>MCP session ID returned by the server on first POST / MCP 服务器首次 POST 返回的会话 ID</summary>
    private string? _sessionId;

    /// <summary>
    /// Gets the name of this MCP client instance.
    /// 获取此 MCP 客户端实例的名称。
    /// </summary>
    public override string Name => _name;

    /// <summary>
    /// Initializes a new instance of <see cref="StreamableHttpMcpClient"/>.
    /// 初始化 StreamableHttpMcpClient 的新实例。
    /// </summary>
    /// <param name="name">Client instance name / 客户端实例名称</param>
    /// <param name="baseUrl">Base URL of the MCP server / MCP 服务器的基础 URL</param>
    /// <param name="http">Optional HTTP client; creates a new one if not provided / 可选的 HTTP 客户端，未提供时创建新实例</param>
    /// <param name="requestTimeout">Optional request timeout; defaults to 30 seconds / 可选的请求超时时间，默认为 30 秒</param>
    /// <exception cref="ArgumentException">Thrown when name or baseUrl is null/empty / 名称为空或 URL 为空时抛出</exception>
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

    /// <summary>
    /// Initializes the MCP session by sending an "initialize" JSON-RPC request.
    /// Sets IsInitialized to true if the server responds successfully.
    /// 通过发送 "initialize" JSON-RPC 请求初始化 MCP 会话。
    /// 如果服务器成功响应，则将 IsInitialized 设置为 true。
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
    /// Lists available tools from the MCP server via "tools/list" JSON-RPC request.
    /// 通过 "tools/list" JSON-RPC 请求从 MCP 服务器列出可用工具。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>List of tool schemas / 工具模式列表</returns>
    public override async Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendJsonRpcAsync("tools/list", null, cancellationToken).ConfigureAwait(false);

        if (response == null || !response.TryGetValue("result", out var resultObj))
            return new List<McpToolSchema>();

            if (resultObj is JsonElement resultEl && resultEl.TryGetProperty("tools", out var toolsEl))
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var tools = JsonSerializer.Deserialize<List<McpToolSchema>>(toolsEl.GetRawText(), opts);
            return (IReadOnlyList<McpToolSchema>)(tools ?? new List<McpToolSchema>());
        }

        return Array.Empty<McpToolSchema>();
    }

    /// <summary>
    /// Calls a specific tool on the MCP server via "tools/call" JSON-RPC request.
    /// 通过 "tools/call" JSON-RPC 请求调用 MCP 服务器上的特定工具。
    /// </summary>
    /// <param name="toolName">Name of the tool to call / 要调用的工具名称</param>
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

    /// <summary>
    /// Sends a JSON-RPC request to the MCP server via HTTP POST and returns the parsed response.
    /// The request includes an "Accept: text/event-stream" header for SSE compatibility.
    /// 通过 HTTP POST 向 MCP 服务器发送 JSON-RPC 请求并返回解析后的响应。
    /// 请求包含 "Accept: text/event-stream" 头以支持 SSE 兼容性。
    /// </summary>
    /// <param name="method">JSON-RPC method name / JSON-RPC 方法名</param>
    /// <param name="parameters">Method parameters (optional) / 方法参数（可选）</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>Parsed response dictionary with "result" and/or "error" keys, or null on timeout / 包含 "result" 和/或 "error" 键的响应字典，超时时返回 null</returns>
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
            ["method"] = method,
            ["params"] = parameters ?? new Dictionary<string, object>()
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_requestTimeout);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));
        httpRequest.Headers.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        if (_sessionId != null)
        {
            httpRequest.Headers.TryAddWithoutValidation("mcp-session-id", _sessionId);
        }

        try
        {
            using var httpResponse = await _http.SendAsync(httpRequest, cts.Token).ConfigureAwait(false);
            // 从响应头中提取 session ID（服务器在首次 initialize 时返回）
            if (httpResponse.Headers.TryGetValues("mcp-session-id", out var sessionValues))
            {
                var sid = sessionValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(sid)) _sessionId = sid;
            }
            httpResponse.EnsureSuccessStatusCode();

            var body = await httpResponse.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

            // 如果响应体以 SSE event/data 开头，提取 data: 行中的 JSON
            if (body.Length > 0 && body[0] != '{' && body[0] != '[')
            {
                var jsonData = ExtractJsonFromSse(body);
                if (jsonData != null) body = jsonData;
            }

            // Parse as JSON-RPC response / 解析为 JSON-RPC 响应
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

    /// <summary>
    /// Extracts JSON content from SSE-formatted text by finding the first line starting with "data: ".
    /// 从 SSE 格式文本中提取首个以 "data: " 开头的行中的 JSON 内容。
    /// </summary>
    private static string? ExtractJsonFromSse(string sseText)
    {
        if (string.IsNullOrWhiteSpace(sseText)) return null;
        using var reader = new System.IO.StringReader(sseText);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith("data: "))
            {
                var json = line.Substring(6).Trim();
                if (json.Length > 0) return json;
            }
        }
        return null;
    }

    /// <summary>
    /// Disposes the underlying HTTP client resources.
    /// 释放底层 HTTP 客户端资源。
    /// </summary>
    public override void Dispose()
    {
        _http.Dispose();
    }
}
