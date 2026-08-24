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
/// Conversation compaction middleware that triggers summarization when context exceeds the window size.
/// 对话压缩中间件，当对话上下文超出窗口大小时触发摘要压缩。
/// </summary>
public sealed class CompactionMiddleware(int maxContextLength = 4096) : IHarnessMiddleware
{
    /// <summary>
    /// Execution order (700).
    /// 执行顺序（700）。
    /// </summary>
    public int Order => 700;

    /// <summary>
    /// Checks context length and marks compaction if it exceeds the configured maximum.
    /// 检查上下文长度，若超出配置的最大值则标记需要压缩。
    /// </summary>
    /// <param name="ctx">Middleware context / 中间件上下文。</param>
    /// <param name="next">Next delegate in the pipeline / 管道中的下一个委托。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        var contextLen = ctx.Items.GetValueOrDefault("context_length") as int? ?? 0;
        if (contextLen > maxContextLength)
        {
            // 触发压缩：标记需要压缩 // Mark compaction needed
            ctx.Items["needs_compaction"] = true;
        }
        await next();
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
