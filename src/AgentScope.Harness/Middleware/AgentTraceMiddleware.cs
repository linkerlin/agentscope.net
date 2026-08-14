namespace AgentScope.Harness.Middleware;

/// <summary>
/// Agent 追踪中间件。对标 Java AgentTraceMiddleware。
/// 记录 agent 调用开始/结束的日志和时间信息。
/// </summary>
public sealed class AgentTraceMiddleware : IHarnessMiddleware
{
    public int Order => 100;

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

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
