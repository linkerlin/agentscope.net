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

using AgentScope.Core.Tool;

namespace AgentScope.Core.Skill;

/// <summary>
/// Central skill management container that discovers, loads, activates, and provides access to skills and their tools.
/// 中央技能管理容器，负责发现、加载、激活技能并提供对技能及其工具的访问。
/// Corresponds to Java: io.agentscope.core.skill.SkillBox
/// </summary>
public class SkillBox
{
    /// <summary>
    /// The underlying skill registry for storing loaded skills.
    /// 用于存储已加载技能的底层注册表。
    /// </summary>
    private readonly SkillRegistry _registry;

    /// <summary>
    /// Dictionary of discovered registered skills, keyed by skill ID (case-insensitive).
    /// 已发现的注册技能字典，以技能 ID 为键（不区分大小写）。
    /// </summary>
    private readonly Dictionary<string, (RegisteredSkill Registered, ISkillRepository Repository)> _registeredSkills =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Dictionary of available tools, keyed by tool name (case-insensitive).
    /// 可用工具字典，以工具名称为键（不区分大小写）。
    /// </summary>
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// List of skill repositories to scan for skills.
    /// 用于扫描技能的知识库列表。
    /// </summary>
    private readonly List<ISkillRepository> _repositories = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SkillBox"/> class.
    /// 初始化 <see cref="SkillBox"/> 类的新实例。
    /// </summary>
    /// <param name="registry">Optional existing registry. If null, a new one is created. / 可选的现有注册表，如果为 null 则创建新的。</param>
    public SkillBox(SkillRegistry? registry = null)
    {
        _registry = registry ?? new SkillRegistry();
    }

    /// <summary>
    /// Gets the underlying skill registry.
    /// 获取底层的技能注册表。
    /// </summary>
    public SkillRegistry Registry => _registry;

    /// <summary>
    /// Adds a skill repository to scan for skills.
    /// 添加一个技能知识库用于扫描技能。
    /// </summary>
    /// <param name="repository">The repository to add. / 要添加的知识库。</param>
    /// <exception cref="ArgumentNullException">Thrown when repository is null. / 当 repository 为 null 时抛出。</exception>
    public void AddRepository(ISkillRepository repository)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        _repositories.Add(repository);
    }

    /// <summary>
    /// Adds a single tool that can be bound to skills.
    /// 添加一个可绑定到技能的工具。
    /// </summary>
    /// <param name="tool">The tool to add. / 要添加的工具。</param>
    /// <exception cref="ArgumentNullException">Thrown when tool is null. / 当 tool 为 null 时抛出。</exception>
    public void AddTool(ITool tool)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        _tools[tool.Name] = tool;
    }

    /// <summary>
    /// Adds multiple tools that can be bound to skills.
    /// 添加多个可绑定到技能的工具。
    /// </summary>
    /// <param name="tools">The tools to add. / 要添加的工具集合。</param>
    /// <exception cref="ArgumentNullException">Thrown when tools is null. / 当 tools 为 null 时抛出。</exception>
    public void AddTools(IEnumerable<ITool> tools)
    {
        if (tools == null)
            throw new ArgumentNullException(nameof(tools));

        foreach (var tool in tools)
            AddTool(tool);
    }

    /// <summary>
    /// Discovers all skills from all registered repositories.
    /// 从所有已注册的知识库中发现所有技能。
    /// </summary>
    /// <returns>A read-only list of discovered registered skills. / 已发现的注册技能只读列表。</returns>
    public IReadOnlyList<RegisteredSkill> Discover()
    {
        _registeredSkills.Clear();

        foreach (var repository in _repositories)
        {
            foreach (var registeredSkill in repository.Scan())
            {
                if (string.IsNullOrWhiteSpace(registeredSkill.Id))
                    continue;

                _registeredSkills[registeredSkill.Id] = (registeredSkill, repository);
            }
        }

        return _registeredSkills.Values
            .Select(item => item.Registered)
            .ToList();
    }

    /// <summary>
    /// Gets the registered skill metadata by ID.
    /// 根据 ID 获取已注册的技能元数据。
    /// </summary>
    /// <param name="skillId">The skill ID. / 技能 ID。</param>
    /// <returns>The registered skill, or null if not found. / 已注册的技能，如果未找到则返回 null。</returns>
    public RegisteredSkill? GetRegistered(string skillId)
    {
        EnsureDiscovered();
        return _registeredSkills.TryGetValue(skillId ?? string.Empty, out var skillEntry)
            ? skillEntry.Registered
            : null;
    }

    /// <summary>
    /// Loads a skill by ID, creating a runtime instance and registering it.
    /// 根据 ID 加载技能，创建运行时实例并注册。
    /// </summary>
    /// <param name="skillId">The skill ID to load. / 要加载的技能 ID。</param>
    /// <returns>The loaded runtime skill instance. / 加载后的运行时技能实例。</returns>
    /// <exception cref="InvalidOperationException">Thrown when the skill is not registered. / 当技能未注册时抛出。</exception>
    public ISkill Load(string skillId)
    {
        EnsureDiscovered();

        if (!_registeredSkills.TryGetValue(skillId ?? string.Empty, out var skillEntry))
            throw new InvalidOperationException($"Skill '{skillId}' is not registered.");

        var existing = _registry.Get(skillEntry.Registered.Id);
        if (existing != null)
            return existing;

        var loadedSkill = skillEntry.Repository.Load(skillEntry.Registered);
        var runtimeSkill = BindRuntimeSkill(skillEntry.Registered, loadedSkill);

        _registry.Register(skillEntry.Registered.Id, runtimeSkill, skillEntry.Registered);
        return runtimeSkill;
    }

    /// <summary>
    /// Loads all discovered skills.
    /// 加载所有已发现的技能。
    /// </summary>
    /// <returns>A read-only list of all loaded skills. / 所有已加载技能的只读列表。</returns>
    public IReadOnlyList<ISkill> LoadAll()
    {
        EnsureDiscovered();

        return _registeredSkills.Keys
            .Select(Load)
            .ToList();
    }

    /// <summary>
    /// Activates a skill by ID (loads it if not already loaded).
    /// 根据 ID 激活技能（如果尚未加载则先加载）。
    /// </summary>
    /// <param name="skillId">The skill ID to activate. / 要激活的技能 ID。</param>
    public void Activate(string skillId)
    {
        Load(skillId);
        _registry.SetActive(skillId, true);
    }

    /// <summary>
    /// Deactivates a skill by ID (loads it if not already loaded).
    /// 根据 ID 停用技能（如果尚未加载则先加载）。
    /// </summary>
    /// <param name="skillId">The skill ID to deactivate. / 要停用的技能 ID。</param>
    public void Deactivate(string skillId)
    {
        Load(skillId);
        _registry.SetActive(skillId, false);
    }

    /// <summary>
    /// Gets all currently active skills.
    /// 获取所有当前激活的技能。
    /// </summary>
    /// <returns>A read-only list of active skills. / 激活技能的只读列表。</returns>
    public IReadOnlyList<ISkill> GetActiveSkills()
    {
        return _registry.GetActiveSkills().ToList();
    }

    /// <summary>
    /// Gets all tools from active skills, deduplicated by name.
    /// 获取所有激活技能中的工具，按名称去重。
    /// </summary>
    /// <returns>A read-only list of active tools. / 激活工具的只读列表。</returns>
    public IReadOnlyList<ITool> GetActiveTools()
    {
        return _registry.GetActiveSkills()
            .SelectMany(skill => skill.Tools)
            .GroupBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    /// <summary>
    /// Binds a loaded skill with its registered metadata and tools to create a runtime skill instance.
    /// 将加载的技能与注册元数据和工具绑定，创建运行时技能实例。
    /// </summary>
    private ISkill BindRuntimeSkill(RegisteredSkill registeredSkill, ISkill loadedSkill)
    {
        if (loadedSkill is MarkdownSkill)
        {
            return new MarkdownSkill(
                registeredSkill,
                BindTools(registeredSkill.ToolNames),
                registeredSkill.IsActiveByDefault);
        }

        loadedSkill.IsActive = registeredSkill.IsActiveByDefault;
        return loadedSkill;
    }

    /// <summary>
    /// Resolves tool instances by their names from the available tools dictionary.
    /// 从可用工具字典中根据名称解析工具实例。
    /// </summary>
    private IReadOnlyList<ITool> BindTools(IEnumerable<string> toolNames)
    {
        var boundTools = new List<ITool>();
        foreach (var toolName in toolNames)
        {
            if (_tools.TryGetValue(toolName, out var tool))
                boundTools.Add(tool);
        }

        return boundTools;
    }

    /// <summary>
    /// Ensures skills have been discovered from repositories before operations.
    /// 确保在执行操作前已从知识库中发现技能。
    /// </summary>
    private void EnsureDiscovered()
    {
        if (_registeredSkills.Count == 0 && _repositories.Count > 0)
            Discover();
    }
}
