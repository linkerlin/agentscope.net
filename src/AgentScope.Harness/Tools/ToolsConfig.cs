namespace AgentScope.Harness.Tools;

public sealed record ToolsConfig
{
    public List<McpServerConfig> McpServers { get; init; } = new();
    public ToolFilter? Filter { get; init; }
}
