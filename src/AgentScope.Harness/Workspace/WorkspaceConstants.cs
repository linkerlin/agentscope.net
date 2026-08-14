namespace AgentScope.Harness.Workspace;

/// <summary>
/// 工作区路径常量。对标 Java WorkspaceConstants。
/// </summary>
public static class WorkspaceConstants
{
    /// <summary>默认工作区根目录。对标 Java <c>DEFAULT_WORKSPACE_ROOT</c>。</summary>
    public const string DefaultWorkspaceRoot = ".agentscope/workspace";

    public const string AgentsMd = "AGENTS.md";
    public const string MemoryMd = "MEMORY.md";
    public const string KnowledgeMd = "KNOWLEDGE.md";
    public const string ToolsJson = "tools.json";

    public const string MemoryDir = "memory";
    public const string WorkspaceDir = "workspace";
    public const string SkillsDir = "skills";
    public const string SubagentsDir = "subagents";
    public const string KnowledgeDir = "knowledge";
    public const string RulesDir = "rules";
    public const string AgentsDir = "agents";
    public const string SessionsDir = "sessions";
    public const string TasksDir = "tasks";
    public const string IndexDir = ".index";

    /// <summary>归档目录（记忆维护把过期日文件移到这里）。</summary>
    public const string MemoryArchiveDir = "memory/archive";

    public const string SessionsStore = "sessions.json";
    public const string SessionContextExt = ".jsonl";
    public const string SessionLogExt = ".log.jsonl";
}
