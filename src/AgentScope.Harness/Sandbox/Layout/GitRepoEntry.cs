namespace AgentScope.Harness.Sandbox.Layout;

public sealed record GitRepoEntry(string RepoUrl, string Branch = "main", string? TargetPath = null, bool Ephemeral = true);
