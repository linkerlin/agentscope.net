using AgentScope.Harness.Sandbox;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// 沙箱生命周期中间件。对标 Java SandboxLifecycleMiddleware。
/// 在每个 Agent 调用前获取沙箱，调用后释放并持久化状态。
/// </summary>
public sealed class SandboxLifecycleMiddleware(
    SandboxManager? manager = null) : IHarnessMiddleware
{
    public int Order => 50;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        if (manager != null)
        {
            var sandboxCtx = SandboxContext.Default;
            ctx.Items["sandbox"] = sandboxCtx;
        }

        try { await next(); }
        finally
        {
            ctx.Items.Remove("sandbox");
        }
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
