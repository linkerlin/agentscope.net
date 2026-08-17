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

public class SkillBox
{
    private readonly SkillRegistry _registry;
    private readonly Dictionary<string, (RegisteredSkill Registered, ISkillRepository Repository)> _registeredSkills =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ISkillRepository> _repositories = new();

    public SkillBox(SkillRegistry? registry = null)
    {
        _registry = registry ?? new SkillRegistry();
    }

    public SkillRegistry Registry => _registry;

    public void AddRepository(ISkillRepository repository)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        _repositories.Add(repository);
    }

    public void AddTool(ITool tool)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        _tools[tool.Name] = tool;
    }

    public void AddTools(IEnumerable<ITool> tools)
    {
        if (tools == null)
            throw new ArgumentNullException(nameof(tools));

        foreach (var tool in tools)
            AddTool(tool);
    }

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

    public RegisteredSkill? GetRegistered(string skillId)
    {
        EnsureDiscovered();
        return _registeredSkills.TryGetValue(skillId ?? string.Empty, out var skillEntry)
            ? skillEntry.Registered
            : null;
    }

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

    public IReadOnlyList<ISkill> LoadAll()
    {
        EnsureDiscovered();

        return _registeredSkills.Keys
            .Select(Load)
            .ToList();
    }

    public void Activate(string skillId)
    {
        Load(skillId);
        _registry.SetActive(skillId, true);
    }

    public void Deactivate(string skillId)
    {
        Load(skillId);
        _registry.SetActive(skillId, false);
    }

    public IReadOnlyList<ISkill> GetActiveSkills()
    {
        return _registry.GetActiveSkills().ToList();
    }

    public IReadOnlyList<ITool> GetActiveTools()
    {
        return _registry.GetActiveSkills()
            .SelectMany(skill => skill.Tools)
            .GroupBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

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

    private void EnsureDiscovered()
    {
        if (_registeredSkills.Count == 0 && _repositories.Count > 0)
            Discover();
    }
}