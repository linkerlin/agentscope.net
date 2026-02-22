using System.Collections.Generic;
using AgentScope.Core.Tool;

namespace AgentScope.Core;

public static class ToolFactory
{
    public static ITool Create(string toolType, Dictionary<string, object>? config = null)
    {
        return toolType.ToLowerInvariant() switch
        {
            "calculator" => new CalculatorTool(),
            "get_time" => new GetTimeTool(),
            "web_search" => new WebSearchTool(),
            "code_execution" => new CodeExecutionTool(),
            _ => throw new NotSupportedException($"Tool type '{toolType}' is not supported")
        };
    }

    public static List<ITool> CreateDefaults()
    {
        return new List<ITool>
        {
            new CalculatorTool(),
            new GetTimeTool(),
            new WebSearchTool(),
            new CodeExecutionTool()
        };
    }
}

public static class ToolFactoryExtensions
{
    public static bool IsSupportedTool(string toolType)
    {
        var supported = new HashSet<string>
        {
            "calculator", "get_time", "web_search", "code_execution"
        };
        return supported.Contains(toolType.ToLowerInvariant());
    }

    public static List<string> GetSupportedTools()
    {
        return new List<string> { "calculator", "get_time", "web_search", "code_execution" };
    }
}
