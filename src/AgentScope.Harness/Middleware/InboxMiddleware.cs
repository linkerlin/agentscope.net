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

using AgentScope.Harness.Bus;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// Inbox processing middleware that handles inbox messages before agent invocation
/// and pushes results after completion.
/// 收件箱处理中间件，在 agent 调用前处理收件箱消息，调用后推送结果。
/// </summary>
public sealed class InboxMiddleware(IMessageBus bus) : IHarnessMiddleware
{
    /// <summary>
    /// Execution order (200).
    /// 执行顺序（200）。
    /// </summary>
    public int Order => 200;

    /// <summary>
    /// Drains inbox messages before the agent call and stores them in Items.
    /// 在 agent 调用前排出收件箱消息并存入 Items。
    /// </summary>
    /// <param name="ctx">Middleware context / 中间件上下文。</param>
    /// <param name="next">Next delegate in the pipeline / 管道中的下一个委托。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        var inbox = ctx.Items.GetValueOrDefault("inbox") as string ?? "default";
        await foreach (var entry in bus.InboxDrainAsync(inbox, ct).ConfigureAwait(false))
        {
            ctx.Items["inbox_message"] = entry;
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
