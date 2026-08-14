using AgentScope.Core.Agent;
using AgentScope.Harness.Subagent;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// 子 Agent 编排中间件。对标 Java SubagentsMiddleware。
/// 在 agent 调用前加载子 agent spec，注入动态子 agent 列表。
/// </summary>
public sealed class SubagentsMiddleware(ISubagentManager manager) : IHarnessMiddleware
{
    public int Order => 300;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        ctx.Items["subagents"] = manager;
        await next();
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
