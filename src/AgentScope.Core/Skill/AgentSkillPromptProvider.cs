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
using System.Linq;
using System.Text;

namespace AgentScope.Core.Skill;

/// <summary>
/// Skill prompt provider: assembles skill description sections that are appended to the system prompt,
/// based on currently active/available skills.
/// 技能提示词提供者：根据当前已激活/可用技能，组装附加到系统提示词的技能说明段落。
/// Corresponds to Java: io.agentscope.core.skill.AgentSkillPromptProvider
/// </summary>
public class AgentSkillPromptProvider
{
    /// <summary>
    /// The skill registry used to query registered skills.
    /// 用于查询已注册技能的技能注册表。
    /// </summary>
    private readonly SkillRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentSkillPromptProvider"/> class.
    /// 初始化 <see cref="AgentSkillPromptProvider"/> 类的新实例。
    /// </summary>
    /// <param name="registry">The skill registry. / 技能注册表。</param>
    /// <exception cref="System.ArgumentNullException">Thrown when registry is null. / 当 registry 为 null 时抛出。</exception>
    public AgentSkillPromptProvider(SkillRegistry registry)
    {
        _registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Builds a markdown section describing each active skill's purpose and available tools.
    /// Returns an empty string when no skills are available.
    /// 生成技能说明段落（描述每个已激活技能的用途与可用工具）。
    /// 无可用技能时返回空字符串。
    /// </summary>
    /// <param name="onlyActive">If true, only includes skills that are active by default. / 若为 true，仅包含默认激活的技能。</param>
    /// <returns>A markdown-formatted skill description section. / 以 Markdown 格式生成的技能说明段落。</returns>
    public string BuildSkillPromptSection(bool onlyActive = true)
    {
        // 查询注册表并过滤技能
        // Query the registry and filter skills
        var skills = (_registry.ListSkills() ?? System.Array.Empty<RegisteredSkill>())
            .Where(s => !onlyActive || s.IsActiveByDefault)
            .ToList();

        if (skills.Count == 0)
        {
            return "";
        }

        // 组装 Markdown 格式的技能说明
        // Assemble a markdown-formatted skill description section
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
