using System.Runtime.CompilerServices;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Harness.Gateway;

/// <summary>
/// Harness 网关实现。对标 Java HarnessGateway。
/// 包装 IAgent 提供流式与非流式入口。
/// </summary>
public sealed class HarnessGateway(IAgent agent) : IGateway
{
    public async Task<Msg> RunAsync(Msg input, RuntimeContext? context = null, CancellationToken ct = default)
    {
        return await agent.CallAsync(input, context);
    }

    public async IAsyncEnumerable<Event> RunStreamAsync(Msg input,
        RuntimeContext? context = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var evt in agent.StreamEventsAsync(input, context))
        {
            ct.ThrowIfCancellationRequested();
            yield return evt;
        }
    }
}
