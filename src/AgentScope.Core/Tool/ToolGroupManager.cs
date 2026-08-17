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

using AgentScope.Core.Formatter;

namespace AgentScope.Core.Tool;

/// <summary>
/// 工具组管理器：注册组、激活/禁用组、获取当前激活的工具名与 Schema。
/// </summary>
public class ToolGroupManager
{
    private readonly Dictionary<string, ToolGroup> _groups = new(StringComparer.OrdinalIgnoreCase);

    public bool HasGroups => _groups.Count > 0;

    public IEnumerable<string> GetActiveGroupNames()
    {
        return _groups.Values
            .Where(group => group.IsActive)
            .Select(group => group.Name)
            .ToArray();
    }

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

    public void SetActiveGroups(IEnumerable<string>? groupNames)
    {
        var activeNames = groupNames?.ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in _groups.Values)
            group.IsActive = activeNames.Contains(group.Name);
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
    /// 根据当前激活组，从工具表中过滤出实际可用工具。
    /// 若未注册任何分组，则返回全部工具，避免对未接入场景造成破坏性影响。
    /// </summary>
    public IReadOnlyDictionary<string, ITool> FilterActiveTools(IReadOnlyDictionary<string, ITool>? toolsByName)
    {
        if (toolsByName == null)
            return new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);

        if (!HasGroups)
            return toolsByName;

        var names = GetActiveToolNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
        var filtered = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            if (toolsByName.TryGetValue(name, out var tool))
                filtered[name] = tool;
        }
        return filtered;
    }

    /// <summary>
    /// 根据当前激活组，从工具表中筛选并生成 ToolSchema 列表（与 Formatter 兼容）。
    /// </summary>
    public List<ToolSchema> GetActiveToolSchemas(IReadOnlyDictionary<string, ITool>? toolsByName)
    {
        var filteredTools = FilterActiveTools(toolsByName);
        if (filteredTools.Count == 0)
            return new List<ToolSchema>();
        var list = new List<ToolSchema>();
        foreach (var (name, tool) in filteredTools)
        {
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
