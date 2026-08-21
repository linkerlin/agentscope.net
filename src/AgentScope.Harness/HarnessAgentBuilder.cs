// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.Model;
using AgentScope.Core.Permission;
using AgentScope.Core.State;
using AgentScope.Core.Tool;
using AgentScope.Core.Skill;
using AgentScope.Core.Tool.File;
using AgentScope.Harness.Bus;
using AgentScope.Harness.Filesystem;
using AgentScope.Harness.Filesystem.Spec;
using AgentScope.Harness.Gateway;
using AgentScope.Harness.Middleware;
using AgentScope.Harness.Subagent;
using AgentScope.Harness.Team;

namespace AgentScope.Harness;

/// <summary>
/// Builder for HarnessAgent. Counterpart to Java HarnessAgentBuilder.
/// HarnessAgent 构建器。对标 Java HarnessAgentBuilder。
/// Provides a fluent API for assembling EnhancedReActAgent with all subsystems.
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

    /// <summary>Sets the agent name. / 设置 Agent 名称。</summary>
    public HarnessAgentBuilder WithName(string name) { _name = name; return this; }
    /// <summary>Sets the system prompt. / 设置系统提示词。</summary>
    public HarnessAgentBuilder WithSystemPrompt(string prompt) { _systemPrompt = prompt; return this; }
    /// <summary>Sets the model. / 设置模型。</summary>
    public HarnessAgentBuilder WithModel(IModel model) { _model = model; return this; }
    /// <summary>Sets the toolkit. / 设置工具包。</summary>
    public HarnessAgentBuilder WithToolkit(Toolkit toolkit) { _toolkit = toolkit; return this; }
    /// <summary>Sets the permission engine. / 设置权限引擎。</summary>
    public HarnessAgentBuilder WithPermission(IPermissionEngine permission) { _permission = permission; return this; }
    /// <summary>Sets the message bus. / 设置消息总线。</summary>
    public HarnessAgentBuilder WithMessageBus(IMessageBus bus) { _bus = bus; return this; }
    /// <summary>Sets the filesystem. / 设置文件系统。</summary>
    public HarnessAgentBuilder WithFilesystem(IFilesystem fs) { _filesystem = fs; return this; }
    /// <summary>Sets the team client. / 设置团队客户端。</summary>
    public HarnessAgentBuilder WithTeamClient(ITeamClient team) { _teamClient = team; return this; }
    /// <summary>Sets the subagent manager. / 设置子 Agent 管理器。</summary>
    public HarnessAgentBuilder WithSubagentManager(ISubagentManager mgr) { _subagentManager = mgr; return this; }
    /// <summary>Adds a middleware to the pipeline. / 添加中间件到管道。</summary>
    public HarnessAgentBuilder WithMiddleware(IHarnessMiddleware mw) { _middlewares.Add(mw); return this; }
    /// <summary>Sets max reasoning iterations. / 设置最大推理迭代次数。</summary>
    public HarnessAgentBuilder WithMaxIterations(int n) { _maxIterations = n; return this; }

    /// <summary>
    /// Sets the workspace manager. Enables workspace context injection, @path expansion, and memory maintenance.
    /// 指定工作区管理器。设置后自动启用工作区上下文注入、@path 展开与记忆维护。
    /// </summary>
    public HarnessAgentBuilder WithWorkspace(Workspace.WorkspaceManager mgr)
    { _workspaceManager = mgr; return this; }

    /// <summary>
    /// Creates a workspace manager from a root directory (convenience overload).
    /// 按根目录创建工作区管理器（便捷重载）。
    /// </summary>
    /// <param name="root">Root directory path. / 根目录路径。</param>
    /// <param name="sandboxed">Whether to enable sandbox mode. / 是否启用沙箱模式。</param>
    public HarnessAgentBuilder WithWorkspaceRoot(string root, bool sandboxed = true)
    { _workspaceManager = new Workspace.WorkspaceManager(root, sandboxed); return this; }

    /// <summary>
    /// Configures tool result eviction policy. Automatically enables ToolResultEvictionMiddleware.
    /// 配置大工具结果驱逐策略；设置后自动启用 ToolResultEvictionMiddleware。
    /// </summary>
    public HarnessAgentBuilder WithToolResultEviction(Memory.Compaction.ToolResultEvictionConfig cfg)
    { _evictionConfig = cfg; return this; }

    /// <summary>
    /// Configures the memory consolidator for periodic memory maintenance middleware calls.
    /// 配置记忆整合器，供记忆维护中间件周期调用。
    /// </summary>
    public HarnessAgentBuilder WithMemoryConsolidator(Memory.MemoryConsolidator c)
    { _consolidator = c; return this; }

    /// <summary>
    /// Configures skill usage statistics store. Automatically enables SkillUsageMiddleware.
    /// 配置技能使用统计存储；设置后自动启用 SkillUsageMiddleware。
    /// </summary>
    public HarnessAgentBuilder WithSkillUsageStore(Skill.Curator.SkillUsageStore store)
    { _skillUsageStore = store; return this; }

    /// <summary>
    /// Configures the skill curator. Automatically enables SkillCuratorMiddleware.
    /// 配置技能策展器；设置后自动启用 SkillCuratorMiddleware。
    /// </summary>
    public HarnessAgentBuilder WithSkillCurator(Skill.Curator.SkillCurator curator)
    { _skillCurator = curator; return this; }

    /// <summary>
    /// Configures a default sandboxed filesystem rooted at the given or current directory.
    /// 配置默认沙箱文件系统，根目录为指定路径或当前目录。
    /// </summary>
    /// <param name="workspaceRoot">Optional workspace root. / 可选的工作区根目录。</param>
    public HarnessAgentBuilder WithDefaultFilesystem(string? workspaceRoot = null)
    {
        _filesystem = new LocalFilesystemSpec()
            .WithRoot(workspaceRoot ?? Directory.GetCurrentDirectory())
            .WithMode(Workspace.LocalFsMode.Sandboxed)
            .Build();
        return this;
    }

    /// <summary>
    /// Builds the HarnessAgent with all configured components and middleware pipeline.
    /// 使用所有已配置的组件和中间件管道构建 HarnessAgent。
    /// </summary>
    /// <returns>A fully constructed HarnessAgent. / 完整构建的 HarnessAgent。</returns>
    /// <exception cref="InvalidOperationException">Thrown when no model is configured. / 未配置模型时抛出。</exception>
    public HarnessAgent Build()
    {
        var bus = _bus ?? new WorkspaceMessageBus();
        var filesystem = _filesystem ?? new LocalFilesystemSpec()
            .WithRoot(Directory.GetCurrentDirectory())
            .Build();
        var teamClient = _teamClient ?? new LocalTeamClient();
        var subagentManager = _subagentManager ?? new DefaultAgentManager();

        // 当配置了工作区时，自动注册默认工具集（文件工具、工作区工具等）。
        // Auto-register default workspace tools when WorkspaceManager is provided.
        if (_workspaceManager != null)
        {
            var wsRoot = Path.GetFullPath(_workspaceManager.WorkspaceRoot);

            // 将工作区根目录加入文件工具沙箱（允许 read_file / write_file 操作工作区文件）
            var currentRoots = new List<string>(FileToolUtils.AllowedRoots);
            var wsRootNormalized = wsRoot.TrimEnd(Path.DirectorySeparatorChar);
            if (!currentRoots.Any(r =>
                {
                    try { return Path.GetFullPath(r).TrimEnd(Path.DirectorySeparatorChar)
                        .Equals(wsRootNormalized, StringComparison.OrdinalIgnoreCase); }
                    catch { return false; }
                }))
            {
                currentRoots.Add(wsRoot);
                FileToolUtils.AllowedRoots = currentRoots.AsReadOnly();
            }

            // 如果没有显式提供 Toolkit，则创建一个默认 Toolkit
            _toolkit ??= new Toolkit();

            // 注册基础文件工具（幂等：已存在同名工具则跳过）
            if (_toolkit.Resolve("read_file") == null)
                _toolkit.AddTool(new ReadFileTool());
            if (_toolkit.Resolve("write_file") == null)
                _toolkit.AddTool(new WriteFileTool());

            // 自动发现并注册 workspace 中的技能（从多个可能的技能目录扫描）
            // Auto-discover and register skills from workspace (skills, .skills, .skill)
            var skillRoots = new[] { "skills", ".skills", ".skill" };
            var parser = new MarkdownSkillParser();
            var allSkills = new List<ISkill>();
            foreach (var subDir in skillRoots)
            {
                var dir = Path.Combine(wsRoot, subDir);
                if (!Directory.Exists(dir)) continue;
                try
                {
                    foreach (var file in Directory.GetFiles(dir, "SKILL.md",
                        SearchOption.AllDirectories))
                    {
                        try
                        {
                            var registered = parser.ParseFile(file);
                            allSkills.Add(new MarkdownSkill(registered, isActive: true));
                        }
                        catch
                        {
                            // 单个技能解析失败不影响其他技能
                        }
                    }
                }
                catch
                {
                    // 目录不可读等错误不影响主流程
                }
            }
            if (allSkills.Count > 0)
            {
                SkillToolFactory.RegisterSkills(_toolkit, allSkills);
            }
        }

        // 构建 EnhancedReActAgent // Build the inner EnhancedReActAgent
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

        // 装配中间件 // Assemble middleware pipeline
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
        // Workspace-related middlewares: only when WorkspaceManager is provided
        if (_workspaceManager != null)
        {
            middlewares.Add(new WorkspaceContextMiddleware(_workspaceManager, _name));
            middlewares.Add(new AtPathExpansionMiddleware(_workspaceManager));
            middlewares.Add(new MemoryMaintenanceMiddleware(_workspaceManager, _consolidator));
        }

        // 大工具结果驱逐：需要显式配置，避免默认改写工具输出
        // Tool result eviction: requires explicit configuration to avoid altering tool output by default
        if (_evictionConfig != null)
            middlewares.Add(new ToolResultEvictionMiddleware(filesystem, _evictionConfig));

        // 技能遥测与生命周期策展 // Skill telemetry and lifecycle curation
        if (_skillUsageStore != null)
            middlewares.Add(new SkillUsageMiddleware(_skillUsageStore));
        if (_skillCurator != null)
            middlewares.Add(new SkillCuratorMiddleware(_skillCurator));

        return new HarnessAgent(inner, bus, filesystem, gateway, middlewares);
    }
}
