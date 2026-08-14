using AgentScope.Core.A2A.Server.Executor.Runner;
using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Server.Executor;

/// <summary>
/// A2A Agent 执行器。对标 Java AgentScopeAgentExecutor。
/// 支持阻塞模式（完整结果）和流式模式（逐个事件发送）。
/// </summary>
public sealed class AgentScopeAgentExecutor(IAgentRunner runner)
{
    public async Task<Msg> ExecuteAsync(IReadOnlyList<Msg> messages, AgentRequestOptions? options = null,
        CancellationToken ct = default)
    {
        Msg? result = null;
        await foreach (var evt in runner.StreamAsync(messages, options ?? new AgentRequestOptions(), ct))
        {
            if (evt.IsLast && evt.Message != null)
                result = evt.Message;
        }
        return result ?? Msg.Builder().Role("assistant").TextContent("").Build();
    }

    public IAsyncEnumerable<Event> StreamAsync(IReadOnlyList<Msg> messages, AgentRequestOptions? options = null,
        CancellationToken ct = default) =>
        runner.StreamAsync(messages, options ?? new AgentRequestOptions(), ct);
}
