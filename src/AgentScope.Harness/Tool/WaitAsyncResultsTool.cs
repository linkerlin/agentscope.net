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
using AgentScope.Core.Tool;

namespace AgentScope.Harness.Tool;

/// <summary>
/// 异步结果等待工具，让 Agent 可以等待异步操作完成。
/// 对标 Java WaitAsyncResultsTool。
/// </summary>
public sealed class WaitAsyncResultsTool : ITool
{
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<string>> PendingResults = new();

    public string Name => "wait_async_result";
    public string Description => "等待异步操作完成并获取结果";

    /// <summary>
    /// 注册一个待等待的异步操作标识。
    /// </summary>
    public static string RegisterPending()
    {
        var token = Guid.NewGuid().ToString("N");
        PendingResults[token] = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        return token;
    }

    /// <summary>
    /// 完成一个异步操作并将结果写入指定 token。
    /// </summary>
    public static bool Complete(string token, string result)
    {
        if (PendingResults.TryRemove(token, out var tcs))
        {
            return tcs.TrySetResult(result);
        }
        return false;
    }

    /// <summary>
    /// 取消一个待等待的异步操作。
    /// </summary>
    public static bool Cancel(string token)
    {
        if (PendingResults.TryRemove(token, out var tcs))
        {
            return tcs.TrySetCanceled();
        }
        return false;
    }

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var token = parameters.GetValueOrDefault("token")?.ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            return ToolResult.Fail("需要 token 参数");
        }

        if (!PendingResults.TryGetValue(token, out var tcs))
        {
            return ToolResult.Fail($"未找到 token: {token}");
        }

        try
        {
            var timeoutSeconds = 30;
            if (parameters.TryGetValue("timeout", out var timeoutObj) &&
                int.TryParse(timeoutObj?.ToString(), out var parsed))
            {
                timeoutSeconds = parsed;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var result = await tcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);
            PendingResults.TryRemove(token, out _);
            return ToolResult.Ok(result);
        }
        catch (TimeoutException)
        {
            return ToolResult.Fail($"等待超时（{token}）");
        }
        catch (OperationCanceledException)
        {
            return ToolResult.Fail($"等待被取消（{token}）");
        }
    }

    public Dictionary<string, object> GetSchema() => new()
    {
        ["name"] = Name,
        ["description"] = Description,
        ["parameters"] = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["token"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "异步操作标识" },
                ["timeout"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "超时秒数（默认 30）" }
            },
            ["required"] = new[] { "token" }
        }
    };
}
