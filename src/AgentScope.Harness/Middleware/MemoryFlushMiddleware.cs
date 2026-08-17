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
/// Memory flush middleware that extracts conversation into long-term memory after agent completion.
/// 内存刷出中间件，Agent 调用完成后将对话提取为长期记忆。
/// </summary>
public sealed class MemoryFlushMiddleware : IHarnessMiddleware
{
    /// <summary>
    /// Execution order (800).
    /// 执行顺序（800）。
    /// </summary>
    public int Order => 800;

    /// <summary>
    /// Invokes next middleware and marks memory flush as pending after completion.
    /// 调用下一中间件，完成后标记需要刷出记忆。
    /// </summary>
    /// <param name="ctx">Middleware context / 中间件上下文。</param>
    /// <param name="next">Next delegate / 下一个委托。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        await next();
        // 调用后触发记忆刷出 // Trigger memory flush after call
        ctx.Items["memory_flush_pending"] = true;
    }

    /// <summary>
    /// Pass-through for model call events.
    /// 模型调用事件直通。
    /// </summary>
    /// <inheritdoc cref="OnAgentAsync(MiddlewareContext, Func{ValueTask}, CancellationToken)" path="/param"/>
    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    /// <summary>
    /// Pass-through for tool execution events.
    /// 工具执行事件直通。
    /// </summary>
    /// <inheritdoc cref="OnAgentAsync(MiddlewareContext, Func{ValueTask}, CancellationToken)" path="/param"/>
    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
