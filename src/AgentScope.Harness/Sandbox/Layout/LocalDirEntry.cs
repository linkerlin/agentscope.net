namespace AgentScope.Harness.Sandbox.Layout;

public sealed record LocalDirEntry(string HostPath, string ContainerPath, bool Ephemeral = false);
