// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tool;

namespace AgentScope.Core.Skill;

public class MarkdownSkill : ISkill
{
    public MarkdownSkill(RegisteredSkill registeredSkill, IReadOnlyList<ITool>? tools = null, bool isActive = true)
    {
        if (registeredSkill == null)
            throw new ArgumentNullException(nameof(registeredSkill));

        Id = registeredSkill.Id;
        Name = registeredSkill.Name;
        Description = registeredSkill.Description;
        SourcePath = registeredSkill.SourcePath;
        RawContent = registeredSkill.RawContent ?? string.Empty;
        ToolNames = registeredSkill.ToolNames.AsReadOnly();
        Tools = tools ?? Array.Empty<ITool>();
        IsActive = isActive;
    }

    public string Id { get; }

    public string Name { get; }

    public string Description { get; }

    public IReadOnlyList<string> ToolNames { get; }

    public IReadOnlyList<ITool> Tools { get; }

    public bool IsActive { get; set; }

    public string? SourcePath { get; }

    public string RawContent { get; }
}