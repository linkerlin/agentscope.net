using AgentScope.Core.Memory;
using AgentScope.Core.Tool;

namespace AgentScope.Harness.Tool;

public sealed class MemorySaveTool(ILongTermMemory memory) : ITool
{
    public string Name => "memory_save";
    public string Description => "保存信息到长期记忆";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var text = parameters.GetValueOrDefault("text")?.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return ToolResult.Fail("需要 text 参数");

        await memory.AddAsync(text);
        return ToolResult.Ok("记忆已保存");
    }

    public Dictionary<string, object> GetSchema() => new()
    {
        ["name"] = Name,
        ["description"] = Description,
        ["parameters"] = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["text"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "要保存的信息" }
            },
            ["required"] = new[] { "text" }
        }
    };
}
