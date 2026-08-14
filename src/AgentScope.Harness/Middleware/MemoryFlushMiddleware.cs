namespace AgentScope.Harness.Middleware;

/// <summary>
/// 内存刷出中间件。对标 Java MemoryFlushMiddleware。
/// Agent 调用完成后将对话提取为长期记忆。
/// </summary>
public sealed class MemoryFlushMiddleware : IHarnessMiddleware
{
    public int Order => 800;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        await next();
        // 调用后触发记忆刷出
        ctx.Items["memory_flush_pending"] = true;
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
