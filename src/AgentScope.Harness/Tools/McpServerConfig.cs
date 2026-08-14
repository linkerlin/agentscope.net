namespace AgentScope.Harness.Tools;

public sealed record McpServerConfig(string Name, string Command, string[]? Args = null, Dictionary<string, string>? Env = null);
