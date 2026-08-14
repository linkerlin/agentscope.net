using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using AgentScope.Core.A2A.Server.Executor;
using AgentScope.Core.A2A.Server.Executor.Runner;
using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Server.Transport.JsonRpc;

/// <summary>
/// JSON-RPC 传输包装器。对标 Java JsonRpcTransportWrapper。
/// 解析 JSON-RPC 请求，路由到对应处理逻辑。
/// </summary>
public sealed class JsonRpcTransportWrapper : ITransportWrapper
{
    private readonly AgentScopeAgentExecutor _executor;
    private readonly IAgentRunner? _runner;
    private readonly ConcurrentDictionary<string, TaskState> _tasks = new();

    public JsonRpcTransportWrapper(AgentScopeAgentExecutor executor, IAgentRunner? runner = null)
    {
        _executor = executor;
        _runner = runner;
    }

    public string TransportType => "jsonrpc";

    public async Task<object> HandleRequestAsync(string body, IDictionary<string, string>? headers = null,
        CancellationToken ct = default)
    {
        try
        {
            var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var method = root.TryGetProperty("method", out var m) ? m.GetString() : "";
            var @params = root.TryGetProperty("params", out var p) ? p : default;
            var id = root.TryGetProperty("id", out var i) ? i : default;

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
            return new { jsonrpc = "2.0", error = new { code = -32700, message = ex.Message }, id = (string?)null };
        }
    }

    private async Task<object> HandleSendTaskAsync(JsonElement? @params, CancellationToken ct)
    {
        if (@params == null) return ErrorResponse(-32602, "Missing params");

        var message = @params?.GetProperty("message");
        var msg = Msg.Builder()
            .Role(message?.TryGetProperty("role", out var role) == true ? role.GetString() : "user")
            .TextContent(message?.TryGetProperty("text", out var text) == true ? text.GetString() : "")
            .Build();

        var taskId = @params?.TryGetProperty("id", out var id) == true ? id.GetString() : null;
        if (!string.IsNullOrEmpty(taskId)) _tasks[taskId] = TaskState.Running;

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

    private async Task<object> HandleSendStreamingTaskAsync(JsonElement? @params, CancellationToken ct)
    {
        if (@params == null) return ErrorResponse(-32602, "Missing params");

        var message = @params?.GetProperty("message");
        var msg = Msg.Builder()
            .Role(message?.TryGetProperty("role", out var role) == true ? role.GetString() : "user")
            .TextContent(message?.TryGetProperty("text", out var text) == true ? text.GetString() : "")
            .Build();

        var taskId = @params?.TryGetProperty("id", out var id) == true ? id.GetString() : null;
        if (!string.IsNullOrEmpty(taskId)) _tasks[taskId] = TaskState.Running;

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

    private Task<object> HandleGetTaskAsync(JsonElement? @params, CancellationToken ct)
    {
        var taskId = @params?.TryGetProperty("id", out var id) == true ? id.GetString() : null;
        var status = taskId != null && _tasks.TryGetValue(taskId, out var s)
            ? s == TaskState.Running ? "running" : "completed"
            : "unknown";
        return Task.FromResult<object>(new
        {
            jsonrpc = "2.0",
            result = new { id = taskId ?? "", status }
        });
    }

    private Task<object> HandleCancelTaskAsync(JsonElement? @params, CancellationToken ct)
    {
        var taskId = @params?.TryGetProperty("id", out var id) == true ? id.GetString() : null;
        if (string.IsNullOrEmpty(taskId))
            return Task.FromResult<object>(ErrorResponse(-32602, "Missing task id"));

        // 真正中断任务：通过 runner 按 taskId 停止对应 Agent
        _runner?.StopAsync(taskId, ct);
        _tasks[taskId] = TaskState.Canceled;
        return Task.FromResult<object>(new
        {
            jsonrpc = "2.0",
            result = new { id = taskId, status = "canceled" }
        });
    }

    private static object ErrorResponse(int code, string message) =>
        new { jsonrpc = "2.0", error = new { code, message } };

    private enum TaskState { Running, Completed, Canceled }
}
