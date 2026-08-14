namespace AgentScope.Harness.Sandbox.Layout;

public sealed record BindMountEntry(string Source, string Target, bool ReadOnly = false, bool Ephemeral = false);
