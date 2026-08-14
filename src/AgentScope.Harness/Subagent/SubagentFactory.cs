using AgentScope.Core.Agent;

namespace AgentScope.Harness.Subagent;

/// <summary>子代理工厂，对应 Java SubagentFactory</summary>
public interface ISubagentFactory
{
    IAgent Create(RuntimeContext parentRc);
}
