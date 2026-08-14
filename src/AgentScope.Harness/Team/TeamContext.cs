namespace AgentScope.Harness.Team;

public sealed record TeamContext(string TeamId, string Name, List<string> MemberIds, Dictionary<string, string> Metadata);
