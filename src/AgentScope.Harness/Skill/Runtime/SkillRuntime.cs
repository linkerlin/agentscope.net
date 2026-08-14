using AgentScope.Core.Tool;

namespace AgentScope.Harness.Skill.Runtime;

/// <summary>技能运行时：聚合 SkillLoadTool 和 SkillPromptBuilder，对应 Java SkillRuntime</summary>
public sealed class SkillRuntime
{
    private readonly SkillPromptBuilder _promptBuilder = new();
    private SkillCatalog _catalog = SkillCatalog.Empty;

    public SkillRuntime() { }

    public SkillRuntime(IEnumerable<HarnessSkillEntry> entries)
    {
        _catalog = SkillCatalog.Of(entries);
    }

    public SkillCatalog CurrentCatalog => _catalog;

    public void Install(SkillCatalog catalog)
    {
        _catalog = catalog;
    }

    public string RenderPrompt(int? maxSkills = null)
    {
        return _promptBuilder.Render(_catalog, maxSkills);
    }

    public void PrepareToolkit(Toolkit toolkit)
    {
        var loadTool = new SkillLoadTool(_catalog.All);
        toolkit.AddTool(loadTool);
    }
}
