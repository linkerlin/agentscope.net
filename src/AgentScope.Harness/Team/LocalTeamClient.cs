// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace AgentScope.Harness.Team;

/// <summary>
/// In-process team orchestration client. Corresponds to Java LocalTeamClient.
/// Uses ConcurrentDictionary + version-based CAS for optimistic concurrency control.
/// 进程内团队编排客户端。使用 ConcurrentDictionary + 版本号实现 CAS 乐观并发控制。
/// </summary>
public sealed class LocalTeamClient : ITeamClient
{
    // 任务存储：taskId -> TeamTask // Task storage: taskId -> TeamTask
    private readonly ConcurrentDictionary<string, TeamTask> _tasks = new();

    // 收件箱存储：memberId -> message queue // Inbox storage: memberId -> message queue
    private readonly ConcurrentDictionary<string, ConcurrentQueue<TeamMessage>> _inboxes = new();

    /// <inheritdoc />
    public Task<string> CreateTaskAsync(TeamTask task, CancellationToken ct = default)
    {
        var id = task.Id;
        _tasks[id] = task with { Status = "pending" };
        return Task.FromResult(id);
    }

    /// <inheritdoc />
    public Task<bool> ClaimTaskAsync(string taskId, string memberId, CancellationToken ct = default)
    {
        // CAS 循环：尝试将 pending 任务更新为 in_progress
        // CAS loop: try to update a pending task to in_progress
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

    /// <inheritdoc />
    public Task CompleteTaskAsync(string taskId, string result, CancellationToken ct = default)
    {
        if (_tasks.TryGetValue(taskId, out var record))
            _tasks[taskId] = record with { Status = "completed", Result = result };
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task FailTaskAsync(string taskId, string error, CancellationToken ct = default)
    {
        if (_tasks.TryGetValue(taskId, out var record))
            _tasks[taskId] = record with { Status = "failed", Result = error };
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TeamTask>> ListTasksAsync(string? memberId = null, CancellationToken ct = default)
    {
        var list = _tasks.Values.AsEnumerable();
        if (memberId != null)
            list = list.Where(t => t.AssignedTo == memberId);
        return Task.FromResult<IReadOnlyList<TeamTask>>(list.ToList());
    }

    /// <inheritdoc />
    public ValueTask SendMessageAsync(string targetMember, TeamMessage message, CancellationToken ct = default)
    {
        var inbox = _inboxes.GetOrAdd(targetMember, _ => new ConcurrentQueue<TeamMessage>());
        inbox.Enqueue(message);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<TeamMessage> ReadMessagesAsync(string inbox, CancellationToken ct = default)
    {
        return ReadInboxAsync(inbox, ct);
    }

    /// <summary>
    /// Reads and dequeues messages from the specified inbox.
    /// 从指定收件箱读取并出队消息。
    /// </summary>
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
