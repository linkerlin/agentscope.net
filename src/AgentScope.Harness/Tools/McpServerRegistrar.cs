namespace AgentScope.Harness.Tools;

public sealed class McpServerRegistrar
{
    private readonly List<McpServerConfig> _servers = new();

    public void Register(McpServerConfig config) => _servers.Add(config);
    public IReadOnlyList<McpServerConfig> Registered => _servers;
    public void Clear() => _servers.Clear();
}
