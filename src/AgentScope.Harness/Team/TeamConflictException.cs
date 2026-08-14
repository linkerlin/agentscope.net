namespace AgentScope.Harness.Team;

public sealed class TeamConflictException : Exception
{
    public TeamConflictException(string message) : base(message) { }
    public TeamConflictException(string message, Exception inner) : base(message, inner) { }
}
