using AgentScope.Core.Tool;

namespace AgentScope.Harness.Tools;

public sealed record ToolFilter(HashSet<string>? AllowedNames = null, HashSet<string>? DeniedNames = null)
{
    public bool IsAllowed(ITool tool)
    {
        if (DeniedNames?.Contains(tool.Name) == true) return false;
        if (AllowedNames != null && !AllowedNames.Contains(tool.Name)) return false;
        return true;
    }
}
