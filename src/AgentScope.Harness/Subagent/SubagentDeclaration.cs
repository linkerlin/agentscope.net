using AgentScope.Core.Agent;

namespace AgentScope.Harness.Subagent;

/// <summary>
/// 子 Agent 声明。对标 Java SubagentDeclaration。
/// 通过 Markdown + YAML front matter 加载。
/// </summary>
public sealed record SubagentDeclaration(
    string Name,
    string Description,
    string? WorkspacePath = null,
    string? InlineBody = null,
    string? RemoteUrl = null,
    WorkspaceMode WorkspaceMode = WorkspaceMode.Shared)
{
    public bool IsRemote => RemoteUrl != null;
}

/// <summary>
/// 子 Agent 工作区模式。对标 Java WorkspaceMode。
/// </summary>
public enum WorkspaceMode
{
    Isolated,
    Shared
}

/// <summary>
/// 子 Agent 工厂委托。对标 Java SubagentFactory。
/// </summary>
public delegate IAgent SubagentFactory(SubagentDeclaration declaration);
