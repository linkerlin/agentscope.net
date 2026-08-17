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

using System.Text;

namespace AgentScope.Harness.Skill.Runtime;

/// <summary>从 SkillCatalog 渲染可用技能提示块，对应 Java SkillPromptBuilder</summary>
public sealed class SkillPromptBuilder
{
    public string Render(SkillCatalog catalog, int? maxSkills = null)
    {
        if (catalog.IsEmpty) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("<available_skills>");

        var skills = catalog.All.AsEnumerable();
        if (maxSkills.HasValue)
            skills = skills.Take(maxSkills.Value);

        foreach (var entry in skills)
        {
            sb.AppendLine($"  <skill id=\"{entry.SkillId}\">");
            sb.AppendLine($"    <name>{entry.Skill.Name}</name>");
            if (!string.IsNullOrEmpty(entry.Skill.Description))
                sb.AppendLine($"    <description>{entry.Skill.Description}</description>");
            sb.AppendLine("  </skill>");
        }

        sb.AppendLine("</available_skills>");
        return sb.ToString();
    }
}
