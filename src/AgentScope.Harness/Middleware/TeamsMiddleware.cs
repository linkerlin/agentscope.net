using AgentScope.Harness.Team;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// 团队协作中间件。对标 Java TeamsMiddleware。
/// 注入当前团队上下文，使 agent 可感知团队任务与成员。
/// </summary>
public sealed class TeamsMiddleware(ITeamClient teams) : IHarnessMiddleware
{
    public int Order => 500;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        ctx.Items["team"] = teams;
        await next();
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
