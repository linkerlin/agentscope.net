namespace AgentScope.Harness.Team;

/// <summary>
/// 团队编排客户端。对标 Java TeamClient。
/// 基于 CAS（Compare-And-Swap）实现乐观并发。
/// </summary>
public interface ITeamClient
{
    Task<string> CreateTaskAsync(TeamTask task, CancellationToken ct = default);
    Task<bool> ClaimTaskAsync(string taskId, string memberId, CancellationToken ct = default);
    Task CompleteTaskAsync(string taskId, string result, CancellationToken ct = default);
    Task FailTaskAsync(string taskId, string error, CancellationToken ct = default);
    Task<IReadOnlyList<TeamTask>> ListTasksAsync(string? memberId = null, CancellationToken ct = default);
    ValueTask SendMessageAsync(string targetMember, TeamMessage message, CancellationToken ct = default);
    IAsyncEnumerable<TeamMessage> ReadMessagesAsync(string inbox, CancellationToken ct = default);
}

public readonly record struct TeamTask(
    string Id, string Description, string AssignedTo = "",
    string Status = "pending", string Result = "", int Version = 1);

public readonly record struct TeamMessage(string From, string To, string Content, DateTime SentAt);
