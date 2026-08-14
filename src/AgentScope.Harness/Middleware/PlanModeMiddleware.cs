namespace AgentScope.Harness.Middleware;

/// <summary>
/// PLAN/BUILD 模式切换中间件。对标 Java PlanModeMiddleware。
/// 在 agent 调用前根据当前模式注入不同的系统提示。
/// </summary>
public sealed class PlanModeMiddleware : IHarnessMiddleware
{
    public int Order => 400;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        var mode = ctx.Items.GetValueOrDefault("plan_mode") as string ?? "build";
        ctx.Items["system_prompt_suffix"] = mode switch
        {
            "plan" => "\n当前模式：规划。请先分解目标为步骤再行动。",
            _ => ""
        };
        await next();
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
