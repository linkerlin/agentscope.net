// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Formatter;

namespace AgentScope.Core.Tool;

/// <summary>
/// 工具组管理器：注册组、激活/禁用组、获取当前激活的工具名与 Schema。
/// </summary>
public class ToolGroupManager
{
    private readonly Dictionary<string, ToolGroup> _groups = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterGroup(ToolGroup group)
    {
        if (group == null) throw new ArgumentNullException(nameof(group));
        _groups[group.Name] = group;
    }

    public void ActivateGroup(string groupName)
    {
        if (_groups.TryGetValue(groupName ?? "", out var g))
            g.IsActive = true;
    }

    public void DeactivateGroup(string groupName)
    {
        if (_groups.TryGetValue(groupName ?? "", out var g))
            g.IsActive = false;
    }

    public IEnumerable<string> GetActiveToolNames()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in _groups.Values.Where(x => x.IsActive))
        {
            foreach (var t in g.GetTools())
                set.Add(t);
        }
        return set;
    }

    /// <summary>
    /// 根据当前激活组，从工具表中筛选并生成 ToolSchema 列表（与 Formatter 兼容）。
    /// </summary>
    public List<ToolSchema> GetActiveToolSchemas(IReadOnlyDictionary<string, ITool>? toolsByName)
    {
        var names = GetActiveToolNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (toolsByName == null || names.Count == 0)
            return new List<ToolSchema>();
        var list = new List<ToolSchema>();
        foreach (var name in names)
        {
            if (!toolsByName.TryGetValue(name, out var tool))
                continue;
            var schema = tool.GetSchema();
            var ts = new ToolSchema
            {
                Name = schema.TryGetValue("name", out var n) ? n?.ToString() ?? tool.Name : tool.Name,
                Description = schema.TryGetValue("description", out var d) ? d?.ToString() : tool.Description,
                Parameters = schema.TryGetValue("parameters", out var p) && p is Dictionary<string, object> dict ? dict : null
            };
            list.Add(ts);
        }
        return list;
    }
}
