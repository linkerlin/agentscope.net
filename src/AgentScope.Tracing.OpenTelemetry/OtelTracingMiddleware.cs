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
    private static readonly ActivitySource Source = new("io.agentscope");
    public int Order => 0;

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

    public async ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        using var activity = Source.StartActivity($"chat {ctx.Model}", ActivityKind.Internal);
        activity?.SetTag("gen_ai.operation.name", "chat");
        activity?.SetTag("gen_ai.request.model", ctx.Model);
        await next();
    }

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
