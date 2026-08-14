namespace AgentScope.Harness.Team;

public sealed record TeamCreateSpec(string Name, string? Description = null, List<string>? MemberIds = null);
