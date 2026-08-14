using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Harness.Subagent;

/// <summary>远程子代理存根，对应 Java RemoteSubagentStub</summary>
public sealed class RemoteSubagentStub : AgentBase
{
    public RemoteSubagentStub(string name, string? description = null)
        : base(name, description ?? $"Remote subagent stub: {name}")
    {
    }

    protected override Task<Msg> DoCallAsync(IReadOnlyList<Msg> messages)
    {
        var msg = Msg.Builder()
            .Role("assistant")
            .Name(Name)
            .TextContent("[此子代理仅支持远程 HTTP 执行，请通过远程传输层调用]")
            .Build();
        return Task.FromResult(msg);
    }
}
