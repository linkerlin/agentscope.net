using AgentScope.Harness.Bus;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// 收件箱处理中间件。对标 Java InboxMiddleware。
/// 在 agent 调用前处理收件箱消息，调用后推送结果。
/// </summary>
public sealed class InboxMiddleware(IMessageBus bus) : IHarnessMiddleware
{
    public int Order => 200;

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

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
