namespace AgentScope.Harness.Sandbox.Layout;

public sealed record LocalFileEntry(string HostPath, string ContainerPath, bool Ephemeral = false);
