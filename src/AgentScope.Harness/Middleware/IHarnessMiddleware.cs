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
/// Harness 中间件接口。扩展 Core MiddlewareBase。
/// Order 决定链中执行顺序（值越小越先执行）。
/// 对标 Java harness 的 20+ 中间件（io.agentscope.harness.agent.middleware.HarnessRuntimeMiddleware）。
/// </summary>
public interface IHarnessMiddleware
{
    int Order { get; }

    ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default);
    ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default);
    ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default);

    /// <summary>
    /// 拦截系统提示词构建。对标 Java <c>Mono&lt;String&gt; onSystemPrompt(Agent, RuntimeContext, String)</c>。
    /// 默认原样返回，需要注入上下文的中间件（如 WorkspaceContextMiddleware）覆写此方法。
    /// </summary>
    ValueTask<string> OnSystemPromptAsync(MiddlewareContext ctx, string prompt, CancellationToken ct = default)
        => ValueTask.FromResult(prompt);
}

/// <summary>
/// 中间件上下文。在链中传递 Agent 调用相关信息。
/// <para>
/// 第 3 轮扩展：原先仅有 AgentName/Model/ToolName/Items，中间件无法读写消息、
/// 工具调用与运行时身份，导致大量中间件只能写成空壳。现补齐对标 Java
/// <c>AgentInput</c> / <c>ReasoningInput</c> / <c>ActingInput</c> 的能力。
/// </para>
/// </summary>
public sealed class MiddlewareContext
{
    public string AgentName { get; init; } = "";
    public string? Model { get; init; }
    public string? ToolName { get; init; }
    public Dictionary<string, object?> Items { get; } = new();

    /// <summary>
    /// 当前回合可见的消息列表（可被中间件就地改写）。
    /// 对标 Java <c>AgentInput.msgs()</c> / <c>ReasoningInput.messages()</c>。
    /// </summary>
    public List<Msg> Messages { get; init; } = [];

    /// <summary>
    /// 本轮待执行的工具调用。对标 Java <c>ActingInput.toolCalls()</c>。
    /// </summary>
    public List<ToolUseBlock> ToolCalls { get; init; } = [];

    /// <summary>运行时上下文（用户 / 会话身份）。</summary>
    public RuntimeContext? Runtime { get; init; }

    /// <summary>用户标识，取自 <see cref="Runtime"/>。</summary>
    public string UserId => Runtime?.UserId ?? "";

    /// <summary>会话标识，优先取 <see cref="Runtime"/>，回退到 Items["session_id"]。</summary>
    public string SessionId =>
        Runtime?.SessionId
        ?? Items.GetValueOrDefault("session_id") as string
        ?? "";
}
