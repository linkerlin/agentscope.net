using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AgentScope.Harness.Team;

/// <summary>
/// 进程内团队编排客户端。对标 Java LocalTeamClient。
/// 使用 ConcurrentDictionary + 版本号实现 CAS 乐观并发控制。
/// </summary>
public sealed class LocalTeamClient : ITeamClient
{
    private readonly ConcurrentDictionary<string, TeamTask> _tasks = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<TeamMessage>> _inboxes = new();

    public Task<string> CreateTaskAsync(TeamTask task, CancellationToken ct = default)
    {
        var id = task.Id;
        _tasks[id] = task with { Status = "pending" };
        return Task.FromResult(id);
    }

    public Task<bool> ClaimTaskAsync(string taskId, string memberId, CancellationToken ct = default)
    {
        while (_tasks.TryGetValue(taskId, out var record) && record.Status == "pending")
        {
            var updated = record with
            {
                AssignedTo = memberId,
                Status = "in_progress",
                Version = record.Version + 1
            };
            if (_tasks.TryUpdate(taskId, updated, record))
                return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task CompleteTaskAsync(string taskId, string result, CancellationToken ct = default)
    {
        if (_tasks.TryGetValue(taskId, out var record))
            _tasks[taskId] = record with { Status = "completed", Result = result };
        return Task.CompletedTask;
    }

    public Task FailTaskAsync(string taskId, string error, CancellationToken ct = default)
    {
        if (_tasks.TryGetValue(taskId, out var record))
            _tasks[taskId] = record with { Status = "failed", Result = error };
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TeamTask>> ListTasksAsync(string? memberId = null, CancellationToken ct = default)
    {
        var list = _tasks.Values.AsEnumerable();
        if (memberId != null)
            list = list.Where(t => t.AssignedTo == memberId);
        return Task.FromResult<IReadOnlyList<TeamTask>>(list.ToList());
    }

    public ValueTask SendMessageAsync(string targetMember, TeamMessage message, CancellationToken ct = default)
    {
        var inbox = _inboxes.GetOrAdd(targetMember, _ => new ConcurrentQueue<TeamMessage>());
        inbox.Enqueue(message);
        return ValueTask.CompletedTask;
    }

    public IAsyncEnumerable<TeamMessage> ReadMessagesAsync(string inbox, CancellationToken ct = default)
    {
        return ReadInboxAsync(inbox, ct);
    }

    private async IAsyncEnumerable<TeamMessage> ReadInboxAsync(string inbox,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (!_inboxes.TryGetValue(inbox, out var queue))
            yield break;

        while (queue.TryDequeue(out var msg))
        {
            ct.ThrowIfCancellationRequested();
            yield return msg;
        }
    }
}
