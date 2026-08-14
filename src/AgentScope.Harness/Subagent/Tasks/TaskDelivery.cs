namespace AgentScope.Harness.Subagent.Tasks;

public sealed record TaskDelivery(
    string TaskId,
    string AgentId,
    TaskStatus Status,
    string? Result,
    string? ErrorMessage,
    DateTime? CompletedAt);

