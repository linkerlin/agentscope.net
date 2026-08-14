using AgentScope.Harness.Transcript;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// 会话转录中间件。对标 Java TranscriptMiddleware。
/// 将每次 agent 调用记录为转录分段。
/// </summary>
public sealed class TranscriptMiddleware(ITranscriptStore store) : IHarnessMiddleware
{
    public int Order => 900;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        var sessionId = ctx.Items.GetValueOrDefault("session_id") as string ?? "default";
        await next();

        var segment = new TranscriptSegment(0, 1, "agent", $"[{DateTime.UtcNow:O}] {ctx.AgentName} 调用完成", DateTime.UtcNow);
        await store.AppendSegmentAsync(sessionId, segment, ct);
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
