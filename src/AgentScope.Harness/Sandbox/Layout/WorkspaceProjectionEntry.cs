namespace AgentScope.Harness.Sandbox.Layout;

public sealed record WorkspaceProjectionEntry(string SourceWorkspace, string TargetPath, bool Ephemeral = true);
