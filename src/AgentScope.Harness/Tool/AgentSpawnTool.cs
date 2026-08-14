using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.Tool;
using AgentScope.Harness.Subagent;

namespace AgentScope.Harness.Tool;

/// <summary>
/// Agent 生成工具。对标 Java AgentGenerateTool/AgentSpawnTool。
/// 在运行中创建新的子 Agent 并执行独立任务。
/// </summary>
public sealed class AgentSpawnTool(ISubagentManager subagentManager) : ITool
{
    public string Name => "spawn_agent";
    public string Description => "生成一个子 Agent 执行独立任务";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var name = parameters.GetValueOrDefault("name")?.ToString();
        if (string.IsNullOrWhiteSpace(name))
            return ToolResult.Fail("需要 name 参数");

        var task = parameters.GetValueOrDefault("task")?.ToString();
        var subagent = subagentManager.GetOrCreate(name);

        // 未指定 task 时仅创建/返回子 Agent（保持原语义）
        if (string.IsNullOrWhiteSpace(task))
            return ToolResult.Ok($"子 Agent '{name}' 已就绪");

        // 真正把任务委派给子 Agent 执行，并回传结果
        var input = Msg.Builder().Role("user").TextContent(task).Build();
        var result = await subagent.CallAsync(input);
        var text = result.GetTextContent() ?? $"子 Agent '{name}' 已完成任务";
        return ToolResult.Ok(text);
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
                ["name"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "子 Agent 名称" },
                ["task"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "任务描述" }
            },
            ["required"] = new[] { "name" }
        }
    };
}
