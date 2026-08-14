using AgentScope.Core.Agent;

namespace AgentScope.Core.A2A.Server.Executor.Runner;

/// <summary>
/// 默认 AgentRunner 实现。对标 Java ReActAgentWithBuilderRunner。
/// 每次调用使用 Builder 创建新 Agent 实例；taskId 缓存与中断语义由基类承担。
/// </summary>
public sealed class ReActAgentWithBuilderRunner(Func<IAgent> agentFactory, string name, string description)
    : BaseReActAgentRunner
{
    public override string AgentName => name;
    public override string AgentDescription => description;

    protected override IAgent BuildAgent() => agentFactory();
}
