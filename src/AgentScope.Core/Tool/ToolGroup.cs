// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Tool;

/// <summary>
/// 工具组：按功能分组工具，支持动态激活/禁用，用于权限与能力边界控制。
/// </summary>
public class ToolGroup
{
    private readonly HashSet<string> _tools = new(StringComparer.OrdinalIgnoreCase);

    public string Name { get; }
    public string Description { get; }
    public bool IsActive { get; set; }

    public ToolGroup(string name, string description = "", bool isActive = true)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? "";
        IsActive = isActive;
    }

    public void AddTool(string toolName)
    {
        if (!string.IsNullOrWhiteSpace(toolName))
            _tools.Add(toolName.Trim());
    }

    public void RemoveTool(string toolName)
    {
        if (toolName != null)
            _tools.Remove(toolName);
    }

    public bool ContainsTool(string toolName) => toolName != null && _tools.Contains(toolName);

    public IReadOnlySet<string> GetTools() => _tools;
}
