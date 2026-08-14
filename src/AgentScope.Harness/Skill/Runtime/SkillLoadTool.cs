using AgentScope.Core.Tool;

namespace AgentScope.Harness.Skill.Runtime;

/// <summary>加载 skill 内容工具，对应 Java SkillLoadTool</summary>
public sealed class SkillLoadTool : ITool
{
    private readonly Dictionary<string, HarnessSkillEntry> _catalog;

    public SkillLoadTool(IEnumerable<HarnessSkillEntry> entries)
    {
        _catalog = entries.ToDictionary(e => e.SkillId, e => e);
    }

    public string Name => "load_skill";
    public string Description => "通过 skillId 加载 skill 的完整内容";

    public Dictionary<string, object> GetSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new Dictionary<string, object>
        {
            ["skillId"] = new Dictionary<string, object>
            {
                ["type"] = "string",
                ["description"] = "要加载的 skill ID"
            }
        },
        ["required"] = new[] { "skillId" }
    };

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args)
    {
        if (!args.TryGetValue("skillId", out var skillIdObj) || skillIdObj == null)
            return ToolResult.Fail("缺少 skillId 参数");

        var skillId = skillIdObj.ToString() ?? "";
        if (!_catalog.TryGetValue(skillId, out var entry))
            return ToolResult.Fail($"未找到 skill: {skillId}");

        await Task.CompletedTask;
        return ToolResult.Ok(entry.Skill.RawContent ?? entry.Skill.Description ?? "");
    }
}
