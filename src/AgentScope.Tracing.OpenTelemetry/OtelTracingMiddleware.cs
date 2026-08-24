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

using System.Diagnostics;
using AgentScope.Harness.Middleware;

namespace AgentScope.Tracing.OpenTelemetry;

/// <summary>
/// OpenTelemetry 追踪中间件。对标 Java OtelTracingMiddleware。
/// 使用 ActivitySource/Activity（C# 惯用）替代 Java 的 GlobalOpenTelemetry 全局单例。
/// Activity 通过 AsyncLocal 自动跨 async 传播，无需 Reactor context 全局钩子。
/// </summary>
public sealed class OtelTracingMiddleware : IHarnessMiddleware
{
    /// <summary>
    /// ActivitySource 实例，用于创建 "io.agentscope" 来源的追踪 span
    /// ActivitySource instance for creating trace spans from the "io.agentscope" source
    /// </summary>
    private static readonly ActivitySource Source = new("io.agentscope");

    /// <summary>
    /// 中间件执行顺序（0 为最高优先级）
    /// Middleware execution order (0 is the highest priority)
    /// </summary>
    public int Order => 0;

    /// <summary>
    /// 在 Agent 调用时创建追踪 span，记录 Agent 名称和操作类型
    /// Creates a trace span during agent invocation, recording agent name and operation type
    /// </summary>
    /// <param name="ctx">中间件上下文 / Middleware context</param>
    /// <param name="next">下一个中间件的委托 / Next middleware delegate</param>
    /// <param name="ct">取消令牌 / Cancellation token</param>
    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        using var activity = Source.StartActivity($"invoke_agent {ctx.AgentName}", ActivityKind.Internal);
        activity?.SetTag("gen_ai.operation.name", "invoke_agent");
        activity?.SetTag("gen_ai.agent.name", ctx.AgentName);
        try { await next(); }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.message", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 在模型调用时创建追踪 span，记录模型名称和操作类型
    /// Creates a trace span during model call, recording model name and operation type
    /// </summary>
    /// <param name="ctx">中间件上下文 / Middleware context</param>
    /// <param name="next">下一个中间件的委托 / Next middleware delegate</param>
    /// <param name="ct">取消令牌 / Cancellation token</param>
    public async ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        using var activity = Source.StartActivity($"chat {ctx.Model}", ActivityKind.Internal);
        activity?.SetTag("gen_ai.operation.name", "chat");
        activity?.SetTag("gen_ai.request.model", ctx.Model);
        await next();
    }

    /// <summary>
    /// 工具执行时直接透传（暂不追踪工具执行）
    /// Pass-through for tool execution (no tracing for now)
    /// </summary>
    /// <param name="ctx">中间件上下文 / Middleware context</param>
    /// <param name="next">下一个中间件的委托 / Next middleware delegate</param>
    /// <param name="ct">取消令牌 / Cancellation token</param>
    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
