using AgentScope.Core.Agent;
using AgentScope.Core.Events;
namespace AgentScope.Harness.Middleware;

public sealed class DynamicSubagentsMiddleware : IHarnessMiddleware
{
    public int Order => 300;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => await next();

    public async ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => await next();

    public async ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => await next();
}
