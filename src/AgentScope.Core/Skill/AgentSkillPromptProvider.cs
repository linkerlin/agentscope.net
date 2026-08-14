// Copyright 2024-2026 the original author or authors.
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
using System.Linq;
using System.Text;

namespace AgentScope.Core.Skill;

/// <summary>
/// 技能提示词提供者：根据当前已激活/可用技能，组装附加到系统提示词的技能说明段落。
/// 对应 Java: io.agentscope.core.skill.AgentSkillPromptProvider
/// </summary>
public class AgentSkillPromptProvider
{
    private readonly SkillRegistry _registry;

    public AgentSkillPromptProvider(SkillRegistry registry)
    {
        _registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// 生成技能说明段落（描述每个已激活技能的用途与可用工具）。
    /// 无可用技能时返回空字符串。
    /// </summary>
    public string BuildSkillPromptSection(bool onlyActive = true)
    {
        var skills = (_registry.ListSkills() ?? System.Array.Empty<RegisteredSkill>())
            .Where(s => !onlyActive || s.IsActiveByDefault)
            .ToList();

        if (skills.Count == 0)
        {
            return "";
        }

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("# 可用技能");
        foreach (var skill in skills)
        {
            sb.AppendLine($"## {skill.Name}");
            if (!string.IsNullOrEmpty(skill.Description))
            {
                sb.AppendLine(skill.Description);
            }

            if (skill.ToolNames.Count > 0)
            {
                sb.AppendLine("工具: " + string.Join(", ", skill.ToolNames));
            }
        }

        return sb.ToString();
    }
}
