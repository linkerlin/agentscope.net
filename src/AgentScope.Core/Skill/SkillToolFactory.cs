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

using System.Collections.Generic;
using AgentScope.Core.Tool;

namespace AgentScope.Core.Skill;

/// <summary>
/// 技能工具工厂：把一个技能（ISkill）包装为可注册进 Toolkit 的 ToolGroup + 工具集。
/// 对应 Java: io.agentscope.core.skill.SkillToolFactory
/// </summary>
public static class SkillToolFactory
{
    /// <summary>
    /// 把技能的工具注册到 Toolkit，并返回对应的 ToolGroup。
    /// </summary>
    public static ToolGroup RegisterSkill(Toolkit toolkit, ISkill skill)
    {
        if (toolkit == null) throw new System.ArgumentNullException(nameof(toolkit));
        if (skill == null) throw new System.ArgumentNullException(nameof(skill));

        var group = new ToolGroup(skill.Name, skill.Description);
        foreach (var tool in skill.Tools)
        {
            toolkit.AddTool(tool, skill.Name);
            group.AddTool(tool.Name);
        }

        group.IsActive = skill.IsActive;
        toolkit.AddGroup(group);
        return group;
    }

    /// <summary>
    /// 把多个技能批量注册到 Toolkit，返回创建的 ToolGroup 列表。
    /// </summary>
    public static List<ToolGroup> RegisterSkills(Toolkit toolkit, IEnumerable<ISkill> skills)
    {
        var groups = new List<ToolGroup>();
        foreach (var skill in skills)
        {
            groups.Add(RegisterSkill(toolkit, skill));
        }

        return groups;
    }
}
