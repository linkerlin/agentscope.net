using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 网关接口。对标 Java Gateway。
/// 负责 Agent 消息的路由、子 Agent 桥接、会话串行化。
/// </summary>
public interface IGateway
{
    Task<Msg> RunAsync(Msg input, RuntimeContext? context = null, CancellationToken ct = default);
    IAsyncEnumerable<Event> RunStreamAsync(Msg input, RuntimeContext? context = null, CancellationToken ct = default);
}
