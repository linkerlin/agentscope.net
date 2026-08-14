namespace AgentScope.Harness.Middleware;

/// <summary>
/// 对话压缩中间件。对标 Java CompactionMiddleware。
/// 当对话上下文超出窗口大小时，调用模型生成摘要压缩。
/// </summary>
public sealed class CompactionMiddleware(int maxContextLength = 4096) : IHarnessMiddleware
{
    public int Order => 700;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        var contextLen = ctx.Items.GetValueOrDefault("context_length") as int? ?? 0;
        if (contextLen > maxContextLength)
        {
            // 触发压缩：标记需要压缩
            ctx.Items["needs_compaction"] = true;
        }
        await next();
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
