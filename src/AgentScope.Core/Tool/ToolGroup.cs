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
