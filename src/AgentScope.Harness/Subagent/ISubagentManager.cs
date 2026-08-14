using AgentScope.Core.Agent;

namespace AgentScope.Harness.Subagent;

/// <summary>
/// 子 Agent 管理器。对标 Java DefaultAgentManager。
/// </summary>
public interface ISubagentManager
{
    IAgent GetOrCreate(string specRef);
    void Register(string name, IAgent agent);
    void Remove(string name);
}
