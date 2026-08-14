using AgentScope.Core.Memory;
using AgentScope.Core.Tool;

namespace AgentScope.Harness.Tool;

/// <summary>
/// 记忆搜索工具。对标 Java MemorySearchTool。
/// </summary>
public sealed class MemorySearchTool(ILongTermMemory memory) : ITool
{
    public string Name => "memory_search";
    public string Description => "搜索长期记忆中的信息";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var query = parameters.GetValueOrDefault("query")?.ToString();
        if (string.IsNullOrWhiteSpace(query))
            return ToolResult.Fail("需要 query 参数");

        var topK = 5;
        if (parameters.TryGetValue("topK", out var k) && k is int ki)
            topK = ki;

        var results = await memory.SearchAsync(query, topK);
        return ToolResult.Ok(string.Join("\n---\n", results));
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
                ["query"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "搜索关键词" },
                ["topK"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "返回结果数量" }
            },
            ["required"] = new[] { "query" }
        }
    };
}
