using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.Model;
using AgentScope.Core.Permission;
using AgentScope.Core.State;
using AgentScope.Core.Tool;
using AgentScope.Harness.Bus;
using AgentScope.Harness.Filesystem;
using AgentScope.Harness.Filesystem.Spec;
using AgentScope.Harness.Gateway;
using AgentScope.Harness.Middleware;
using AgentScope.Harness.Subagent;
using AgentScope.Harness.Team;

namespace AgentScope.Harness;

/// <summary>
/// HarnessAgent 构建器。对标 Java HarnessAgentBuilder。
/// 提供流畅的构建体验，装配 EnhancedReActAgent + 各子系统。
/// </summary>
public sealed class HarnessAgentBuilder
{
    private string _name = "harness-agent";
    private string? _systemPrompt;
    private IModel? _model;
    private Toolkit? _toolkit;
    private IPermissionEngine? _permission;
    private IMessageBus? _bus;
    private IFilesystem? _filesystem;
    private ITeamClient? _teamClient;
    private ISubagentManager? _subagentManager;
    private readonly List<IHarnessMiddleware> _middlewares = [];
    private int _maxIterations = 10;
    private Workspace.WorkspaceManager? _workspaceManager;
    private Memory.Compaction.ToolResultEvictionConfig? _evictionConfig;
    private Memory.MemoryConsolidator? _consolidator;
    private Skill.Curator.SkillUsageStore? _skillUsageStore;
    private Skill.Curator.SkillCurator? _skillCurator;

    public HarnessAgentBuilder WithName(string name) { _name = name; return this; }
    public HarnessAgentBuilder WithSystemPrompt(string prompt) { _systemPrompt = prompt; return this; }
    public HarnessAgentBuilder WithModel(IModel model) { _model = model; return this; }
    public HarnessAgentBuilder WithToolkit(Toolkit toolkit) { _toolkit = toolkit; return this; }
    public HarnessAgentBuilder WithPermission(IPermissionEngine permission) { _permission = permission; return this; }
    public HarnessAgentBuilder WithMessageBus(IMessageBus bus) { _bus = bus; return this; }
    public HarnessAgentBuilder WithFilesystem(IFilesystem fs) { _filesystem = fs; return this; }
    public HarnessAgentBuilder WithTeamClient(ITeamClient team) { _teamClient = team; return this; }
    public HarnessAgentBuilder WithSubagentManager(ISubagentManager mgr) { _subagentManager = mgr; return this; }
    public HarnessAgentBuilder WithMiddleware(IHarnessMiddleware mw) { _middlewares.Add(mw); return this; }
    public HarnessAgentBuilder WithMaxIterations(int n) { _maxIterations = n; return this; }

    /// <summary>指定工作区管理器。设置后自动启用工作区上下文注入、@path 展开与记忆维护。</summary>
    public HarnessAgentBuilder WithWorkspace(Workspace.WorkspaceManager mgr)
    { _workspaceManager = mgr; return this; }

    /// <summary>按根目录创建工作区管理器（便捷重载）。</summary>
    public HarnessAgentBuilder WithWorkspaceRoot(string root, bool sandboxed = true)
    { _workspaceManager = new Workspace.WorkspaceManager(root, sandboxed); return this; }

    /// <summary>配置大工具结果驱逐策略；设置后自动启用 ToolResultEvictionMiddleware。</summary>
    public HarnessAgentBuilder WithToolResultEviction(Memory.Compaction.ToolResultEvictionConfig cfg)
    { _evictionConfig = cfg; return this; }

    /// <summary>配置记忆整合器，供记忆维护中间件周期调用。</summary>
    public HarnessAgentBuilder WithMemoryConsolidator(Memory.MemoryConsolidator c)
    { _consolidator = c; return this; }

    /// <summary>配置技能使用统计存储；设置后自动启用 SkillUsageMiddleware。</summary>
    public HarnessAgentBuilder WithSkillUsageStore(Skill.Curator.SkillUsageStore store)
    { _skillUsageStore = store; return this; }

    /// <summary>配置技能策展器；设置后自动启用 SkillCuratorMiddleware。</summary>
    public HarnessAgentBuilder WithSkillCurator(Skill.Curator.SkillCurator curator)
    { _skillCurator = curator; return this; }

    public HarnessAgentBuilder WithDefaultFilesystem(string? workspaceRoot = null)
    {
        _filesystem = new LocalFilesystemSpec()
            .WithRoot(workspaceRoot ?? Directory.GetCurrentDirectory())
            .WithMode(Workspace.LocalFsMode.Sandboxed)
            .Build();
        return this;
    }

    public HarnessAgent Build()
    {
        var bus = _bus ?? new WorkspaceMessageBus();
        var filesystem = _filesystem ?? new LocalFilesystemSpec()
            .WithRoot(Directory.GetCurrentDirectory())
            .Build();
        var teamClient = _teamClient ?? new LocalTeamClient();
        var subagentManager = _subagentManager ?? new DefaultAgentManager();

        // 构建 EnhancedReActAgent
        var innerBuilder = new EnhancedReActAgentBuilder()
            .Name(_name)
            .SysPrompt(_systemPrompt ?? "You are a helpful AI assistant.")
            .Model(_model ?? throw new InvalidOperationException("必须指定模型"));

        if (_toolkit != null)
        {
            foreach (var tool in _toolkit.AllTools)
                innerBuilder.AddTool(tool);
        }
        if (_permission != null) innerBuilder.PermissionEngine(_permission);
        innerBuilder.MaxIterations(_maxIterations);

        var inner = (EnhancedReActAgent)innerBuilder.Build();

        var gateway = new HarnessGateway(inner);

        // 装配中间件
        var middlewares = new List<IHarnessMiddleware>();
        if (_middlewares.Count > 0) middlewares.AddRange(_middlewares);
        middlewares.Add(new SandboxLifecycleMiddleware());
        middlewares.Add(new SubagentsMiddleware(subagentManager));
        middlewares.Add(new TeamsMiddleware(teamClient));
        middlewares.Add(new InboxMiddleware(bus));
        middlewares.Add(new PlanModeMiddleware());
        middlewares.Add(new CompactionMiddleware());
        middlewares.Add(new MemoryFlushMiddleware());
        middlewares.Add(new AgentTraceMiddleware());
        middlewares.Add(new TranscriptMiddleware(
            new Transcript.FilesystemTranscriptStore("transcripts")));

        // 工作区相关中间件：仅在提供了 WorkspaceManager 时装配
        if (_workspaceManager != null)
        {
            middlewares.Add(new WorkspaceContextMiddleware(_workspaceManager, _name));
            middlewares.Add(new AtPathExpansionMiddleware(_workspaceManager));
            middlewares.Add(new MemoryMaintenanceMiddleware(_workspaceManager, _consolidator));
        }

        // 大工具结果驱逐：需要显式配置，避免默认改写工具输出
        if (_evictionConfig != null)
            middlewares.Add(new ToolResultEvictionMiddleware(filesystem, _evictionConfig));

        // 技能遥测与生命周期策展
        if (_skillUsageStore != null)
            middlewares.Add(new SkillUsageMiddleware(_skillUsageStore));
        if (_skillCurator != null)
            middlewares.Add(new SkillCuratorMiddleware(_skillCurator));

        return new HarnessAgent(inner, bus, filesystem, gateway, middlewares);
    }
}
