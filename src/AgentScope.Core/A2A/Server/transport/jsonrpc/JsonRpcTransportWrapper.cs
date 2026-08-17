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
using System.Text;
using System.Text.Json;
using AgentScope.Core.A2A.Server.Executor;
using AgentScope.Core.A2A.Server.Executor.Runner;
using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Server.Transport.JsonRpc;

/// <summary>
/// JSON-RPC transport wrapper. Counterpart to Java JsonRpcTransportWrapper.
/// Parses JSON-RPC requests and routes them to the corresponding handler.
/// JSON-RPC 传输包装器。对标 Java JsonRpcTransportWrapper。
/// 解析 JSON-RPC 请求，路由到对应处理逻辑。
/// </summary>
public sealed class JsonRpcTransportWrapper : ITransportWrapper
{
    private readonly AgentScopeAgentExecutor _executor;
    private readonly IAgentRunner? _runner;
    private readonly ConcurrentDictionary<string, TaskState> _tasks = new();

    /// <summary>
    /// Initializes a new instance of <see cref="JsonRpcTransportWrapper"/>.
    /// 初始化 <see cref="JsonRpcTransportWrapper"/> 的新实例。
    /// </summary>
    /// <param name="executor">Agent executor for handling tasks / Agent 执行器</param>
    /// <param name="runner">Optional agent runner for cancellation / 可选的 Agent 运行器（用于取消）</param>
    public JsonRpcTransportWrapper(AgentScopeAgentExecutor executor, IAgentRunner? runner = null)
    {
        _executor = executor;
        _runner = runner;
    }

    /// <inheritdoc />
    public string TransportType => "jsonrpc";

    /// <inheritdoc />
    public async Task<object> HandleRequestAsync(string body, IDictionary<string, string>? headers = null,
        CancellationToken ct = default)
    {
        try
        {
            // Parse the JSON-RPC request: extract method, params, and id
            // 解析 JSON-RPC 请求：提取 method、params 和 id
            var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var method = root.TryGetProperty("method", out var m) ? m.GetString() : "";
            var @params = root.TryGetProperty("params", out var p) ? p : default;
            var id = root.TryGetProperty("id", out var i) ? i : default;

            // Route to the appropriate handler based on the method name
            // 根据方法名路由到对应的处理程序
            return method switch
            {
                "tasks/send" => await HandleSendTaskAsync(@params, ct),
                "tasks/sendStreaming" => await HandleSendStreamingTaskAsync(@params, ct),
                "tasks/get" => await HandleGetTaskAsync(@params, ct),
                "tasks/cancel" => await HandleCancelTaskAsync(@params, ct),
                _ => new { jsonrpc = "2.0", error = new { code = -32601, message = $"Method not found: {method}" }, id }
            };
        }
        catch (System.Exception ex)
        {
            // JSON-RPC parse error (-32700)
            // JSON-RPC 解析错误
            return new { jsonrpc = "2.0", error = new { code = -32700, message = ex.Message }, id = (string?)null };
        }
    }

    /// <summary>
    /// Handles the "tasks/send" method: executes a task synchronously.
    /// 处理 "tasks/send" 方法：同步执行任务。
    /// </summary>
    private async Task<object> HandleSendTaskAsync(JsonElement? @params, CancellationToken ct)
    {
        if (@params == null) return ErrorResponse(-32602, "Missing params");

        // Extract message from params and build Msg object
        // 从参数中提取消息并构建 Msg 对象
        var message = @params?.GetProperty("message");
        var msg = Msg.Builder()
            .Role(message?.TryGetProperty("role", out var role) == true ? role.GetString() : "user")
            .TextContent(message?.TryGetProperty("text", out var text) == true ? text.GetString() : "")
            .Build();

        var taskId = @params?.TryGetProperty("id", out var id) == true ? id.GetString() : null;
        if (!string.IsNullOrEmpty(taskId)) _tasks[taskId] = TaskState.Running;

        // Execute the task and get the result
        // 执行任务并获取结果
        var result = await _executor.ExecuteAsync([msg], ct: ct);

        if (!string.IsNullOrEmpty(taskId)) _tasks[taskId] = TaskState.Completed;
        return new
        {
            jsonrpc = "2.0",
            result = new
            {
                id = taskId ?? "",
                status = "completed",
                message = new { role = "assistant", content = result.GetTextContent() }
            }
        };
    }

    /// <summary>
    /// Handles the "tasks/sendStreaming" method: executes a task with streaming response.
    /// 处理 "tasks/sendStreaming" 方法：以流式方式执行任务。
    /// </summary>
    private async Task<object> HandleSendStreamingTaskAsync(JsonElement? @params, CancellationToken ct)
    {
        if (@params == null) return ErrorResponse(-32602, "Missing params");

        // Extract message from params and build Msg object
        // 从参数中提取消息并构建 Msg 对象
        var message = @params?.GetProperty("message");
        var msg = Msg.Builder()
            .Role(message?.TryGetProperty("role", out var role) == true ? role.GetString() : "user")
            .TextContent(message?.TryGetProperty("text", out var text) == true ? text.GetString() : "")
            .Build();

        var taskId = @params?.TryGetProperty("id", out var id) == true ? id.GetString() : null;
        if (!string.IsNullOrEmpty(taskId)) _tasks[taskId] = TaskState.Running;

        // Streaming aggregation: consume events one by one and concatenate the final text
        // 流式聚合：逐个消费事件，拼接最终文本
        var content = new StringBuilder();
        var options = new AgentRequestOptions(taskId);
        await foreach (var evt in _executor.StreamAsync([msg], options, ct))
        {
            var chunk = evt.Message?.GetTextContent();
            if (!string.IsNullOrEmpty(chunk)) content.Append(chunk);
        }

        if (!string.IsNullOrEmpty(taskId)) _tasks[taskId] = TaskState.Completed;
        return new
        {
            jsonrpc = "2.0",
            result = new
            {
                id = taskId ?? "",
                status = "completed",
                message = new { role = "assistant", content = content.ToString() }
            }
        };
    }

    /// <summary>
    /// Handles the "tasks/get" method: queries the status of a task.
    /// 处理 "tasks/get" 方法：查询任务状态。
    /// </summary>
    private Task<object> HandleGetTaskAsync(JsonElement? @params, CancellationToken ct)
    {
        var taskId = @params?.TryGetProperty("id", out var id) == true ? id.GetString() : null;
        // Look up the task state; default to "unknown" if not found
        // 查询任务状态；如果找不到则默认为 "unknown"
        var status = taskId != null && _tasks.TryGetValue(taskId, out var s)
            ? s == TaskState.Running ? "running" : "completed"
            : "unknown";
        return Task.FromResult<object>(new
        {
            jsonrpc = "2.0",
            result = new { id = taskId ?? "", status }
        });
    }

    /// <summary>
    /// Handles the "tasks/cancel" method: cancels a running task via the runner.
    /// 处理 "tasks/cancel" 方法：通过 runner 取消正在运行的任务。
    /// </summary>
    private Task<object> HandleCancelTaskAsync(JsonElement? @params, CancellationToken ct)
    {
        var taskId = @params?.TryGetProperty("id", out var id) == true ? id.GetString() : null;
        if (string.IsNullOrEmpty(taskId))
            return Task.FromResult<object>(ErrorResponse(-32602, "Missing task id"));

        // Actually interrupt the task: stop the corresponding Agent via runner by taskId
        // 真正中断任务：通过 runner 按 taskId 停止对应 Agent
        _runner?.StopAsync(taskId, ct);
        _tasks[taskId] = TaskState.Canceled;
        return Task.FromResult<object>(new
        {
            jsonrpc = "2.0",
            result = new { id = taskId, status = "canceled" }
        });
    }

    /// <summary>
    /// Creates a JSON-RPC error response object.
    /// 创建一个 JSON-RPC 错误响应对象。
    /// </summary>
    private static object ErrorResponse(int code, string message) =>
        new { jsonrpc = "2.0", error = new { code, message } };

    /// <summary>
    /// Internal task state machine.
    /// 内部任务状态机。
    /// </summary>
    private enum TaskState { Running, Completed, Canceled }
}
