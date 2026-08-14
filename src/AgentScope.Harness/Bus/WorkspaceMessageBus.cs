using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace AgentScope.Harness.Bus;

/// <summary>
/// 进程内消息总线实现。对标 Java WorkspaceMessageBus。
/// 使用 Channel&lt;T&gt; 提供背压安全的生产者-消费者模型。
/// </summary>
public sealed class WorkspaceMessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<string, Channel<BusEntry>> _queues = new();
    private readonly ConcurrentDictionary<string, List<LogEntry>> _logs = new();
    private readonly ConcurrentDictionary<string, List<Func<BusEntry, Task>>> _subscribers = new();
    private readonly object _logLock = new();

    // ── Mode A: Drain queue ──

    public ValueTask QueuePushAsync(string queue, BusEntry entry, CancellationToken ct = default)
    {
        var ch = _queues.GetOrAdd(queue, _ => Channel.CreateUnbounded<BusEntry>());
        return ch.Writer.WriteAsync(entry, ct);
    }

    public IAsyncEnumerable<BusEntry> QueueDrainAsync(string queue, CancellationToken ct = default)
    {
        var ch = _queues.GetOrAdd(queue, _ => Channel.CreateUnbounded<BusEntry>());
        return ch.Reader.ReadAllAsync(ct);
    }

    public ValueTask QueueDeleteAsync(string queue, string entryId) =>
        ValueTask.CompletedTask; // Channel<T> 不支持随机删除

    // ── Mode C: Replay log ──

    public ValueTask LogAppendAsync(string log, BusEntry entry, CancellationToken ct = default)
    {
        lock (_logLock)
        {
            var list = _logs.GetOrAdd(log, _ => []);
            var seq = list.Count;
            list.Add(new LogEntry(seq, entry));
        }
        return ValueTask.CompletedTask;
    }

    public IAsyncEnumerable<BusEntry> LogReadAsync(string log, long startSeq, CancellationToken ct = default)
    {
        return ReadLogEntriesAsync(log, startSeq, ct);
    }

    private async IAsyncEnumerable<BusEntry> ReadLogEntriesAsync(string log, long startSeq,
        [EnumeratorCancellation] CancellationToken ct)
    {
        List<LogEntry> snapshot;
        lock (_logLock)
        {
            if (!_logs.TryGetValue(log, out var list))
                yield break;
            snapshot = [.. list];
        }

        foreach (var entry in snapshot.Skip((int)startSeq))
        {
            ct.ThrowIfCancellationRequested();
            yield return entry.Entry with { Sequence = entry.Sequence };
        }
    }

    public ValueTask LogTrimAsync(string log, long upToSeq)
    {
        lock (_logLock)
        {
            if (_logs.TryGetValue(log, out var list))
            {
                if (upToSeq >= list.Count)
                    _logs.TryRemove(log, out _);
                else
                    list.RemoveRange(0, (int)upToSeq);
            }
        }
        return ValueTask.CompletedTask;
    }

    // ── Mode D: Pub/Sub ──

    public ValueTask PublishAsync(string topic, BusEntry entry, CancellationToken ct = default)
    {
        if (_subscribers.TryGetValue(topic, out var handlers))
        {
            foreach (var handler in handlers)
                _ = handler(entry);
        }
        return ValueTask.CompletedTask;
    }

    public IDisposable Subscribe(string topic, Func<BusEntry, Task> handler)
    {
        _subscribers.AddOrUpdate(topic,
            _ => [handler],
            (_, list) => { list.Add(handler); return list; });
        return new Subscription(() => Unsubscribe(topic, handler));
    }

    private void Unsubscribe(string topic, Func<BusEntry, Task> handler)
    {
        if (_subscribers.TryGetValue(topic, out var list))
        {
            list.Remove(handler);
            if (list.Count == 0)
                _subscribers.TryRemove(topic, out _);
        }
    }

    // ── Inbox ──

    public ValueTask InboxPushAsync(string agentId, BusEntry entry, CancellationToken ct = default)
        => QueuePushAsync($"inbox:{agentId}", entry, ct);

    public IAsyncEnumerable<BusEntry> InboxDrainAsync(string agentId, CancellationToken ct = default)
        => QueueDrainAsync($"inbox:{agentId}", ct);

    public async ValueTask DisposeAsync()
    {
        _queues.Clear();
        _logs.Clear();
        _subscribers.Clear();
    }

    private sealed record LogEntry(long Sequence, BusEntry Entry);

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        public void Dispose() => unsubscribe();
    }
}
