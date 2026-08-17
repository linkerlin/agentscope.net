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

using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// Harness middleware interface extending the Core MiddlewareBase.
/// Order determines execution priority in the pipeline (lower values run first).
/// Harness 中间件接口，扩展 Core MiddlewareBase。
/// Order 决定链中执行顺序（值越小越先执行）。
/// </summary>
public interface IHarnessMiddleware
{
    /// <summary>
    /// Execution priority. Lower values execute first.
    /// 执行优先级，值越小越先执行。
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Intercepts agent execution events.
    /// 拦截 Agent 执行事件。
    /// </summary>
    /// <param name="ctx">Middleware context / 中间件上下文。</param>
    /// <param name="next">Next delegate in the pipeline / 管道中的下一个委托。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default);

    /// <summary>
    /// Intercepts model call events.
    /// 拦截模型调用事件。
    /// </summary>
    /// <inheritdoc cref="OnAgentAsync(MiddlewareContext, Func{ValueTask}, CancellationToken)" path="/param"/>
    ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default);

    /// <summary>
    /// Intercepts tool execution events.
    /// 拦截工具执行事件。
    /// </summary>
    /// <inheritdoc cref="OnAgentAsync(MiddlewareContext, Func{ValueTask}, CancellationToken)" path="/param"/>
    ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default);

    /// <summary>
    /// Intercepts and customizes system prompt construction.
    /// Default implementation returns the prompt unchanged.
    /// 拦截系统提示词构建，默认实现原样返回提示词。
    /// Override in middleware that needs to inject context (e.g. WorkspaceContextMiddleware).
    /// 需要注入上下文的中间件（如 WorkspaceContextMiddleware）覆写此方法。
    /// </summary>
    /// <param name="ctx">Middleware context / 中间件上下文。</param>
    /// <param name="prompt">Original system prompt / 原始系统提示词。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>Modified or original prompt / 修改后或原始的提示词。</returns>
    ValueTask<string> OnSystemPromptAsync(MiddlewareContext ctx, string prompt, CancellationToken ct = default)
        => ValueTask.FromResult(prompt);
}

/// <summary>
/// Middleware context that carries agent call information across the pipeline chain.
/// Middleware can read and modify messages, tool calls, and runtime identity.
/// 中间件上下文，在管道链中传递 Agent 调用相关信息。
/// 中间件可以读写消息、工具调用与运行时身份。
/// </summary>
public sealed class MiddlewareContext
{
    /// <summary>
    /// Name of the agent being invoked.
    /// 被调用的 Agent 名称。
    /// </summary>
    public string AgentName { get; init; } = "";

    /// <summary>
    /// Optional model identifier.
    /// 可选的模型标识。
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Optional tool name.
    /// 可选的工具名称。
    /// </summary>
    public string? ToolName { get; init; }

    /// <summary>
    /// Key-value store for arbitrary middleware data sharing.
    /// 任意中间件数据共享的键值存储。
    /// </summary>
    public Dictionary<string, object?> Items { get; } = new();

    /// <summary>
    /// Messages visible in the current turn. Middleware can modify in-place.
    /// 当前回合可见的消息列表，中间件可就地改写。
    /// </summary>
    public List<Msg> Messages { get; init; } = [];

    /// <summary>
    /// Tool calls pending execution in this turn.
    /// 本轮待执行的工具调用。
    /// </summary>
    public List<ToolUseBlock> ToolCalls { get; init; } = [];

    /// <summary>
    /// Runtime context (user/session identity).
    /// 运行时上下文（用户/会话身份）。
    /// </summary>
    public RuntimeContext? Runtime { get; init; }

    /// <summary>
    /// User identifier, sourced from <see cref="Runtime"/>.
    /// 用户标识，取自 <see cref="Runtime"/>。
    /// </summary>
    public string UserId => Runtime?.UserId ?? "";

    /// <summary>
    /// Session identifier, preferring <see cref="Runtime"/>, fallback to Items["session_id"].
    /// 会话标识，优先取 <see cref="Runtime"/>，回退到 Items["session_id"]。
    /// </summary>
    public string SessionId =>
        Runtime?.SessionId
        ?? Items.GetValueOrDefault("session_id") as string
        ?? "";
}
