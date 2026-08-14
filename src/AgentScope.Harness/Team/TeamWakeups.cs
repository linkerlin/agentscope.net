namespace AgentScope.Harness.Team;

public sealed record TeamWakeup(string AgentId, string FromAgentId, string? Message = null, DateTime? SentAt = null);
