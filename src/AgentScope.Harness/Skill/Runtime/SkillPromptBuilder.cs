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
