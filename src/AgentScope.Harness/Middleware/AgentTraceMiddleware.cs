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

namespace AgentScope.Harness.Middleware;

/// <summary>
/// Agent tracing middleware that logs the start/end of agent calls with timing information.
/// Agent 追踪中间件，记录 agent 调用开始/结束的日志和时间信息。
/// </summary>
public sealed class AgentTraceMiddleware : IHarnessMiddleware
{
    /// <summary>
    /// Execution order (100). Higher priority than most middleware.
    /// 执行顺序（100），优先级高于大多数中间件。
    /// </summary>
    public int Order => 100;

    /// <summary>
    /// Logs agent call start and end with elapsed time.
    /// 记录 agent 调用开始和结束，包含耗时信息。
    /// </summary>
    /// <param name="ctx">Middleware context / 中间件上下文。</param>
    /// <param name="next">Next delegate in the pipeline / 管道中的下一个委托。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        var start = DateTime.UtcNow;
        Console.WriteLine($"[Trace] Agent '{ctx.AgentName}' 开始调用");
        try { await next(); }
        finally
        {
            var elapsed = DateTime.UtcNow - start;
            Console.WriteLine($"[Trace] Agent '{ctx.AgentName}' 完成，耗时 {elapsed.TotalMilliseconds:F0}ms");
        }
    }

    /// <summary>
    /// Pass-through for model call events. No tracing applied.
    /// 模型调用事件直通，不执行追踪。
    /// </summary>
    /// <inheritdoc cref="OnAgentAsync(MiddlewareContext, Func{ValueTask}, CancellationToken)" path="/param"/>
    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    /// <summary>
    /// Pass-through for tool execution events. No tracing applied.
    /// 工具执行事件直通，不执行追踪。
    /// </summary>
    /// <inheritdoc cref="OnAgentAsync(MiddlewareContext, Func{ValueTask}, CancellationToken)" path="/param"/>
    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
