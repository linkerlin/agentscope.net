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
using System.Threading.Channels;

namespace AgentScope.Harness.Bus;

/// <summary>
/// In-process message bus implementation, corresponding to Java WorkspaceMessageBus.<br />
/// 进程内消息总线实现，对标 Java WorkspaceMessageBus。<br />
/// Uses <see cref="System.Threading.Channels.Channel{T}"/> to provide a backpressure-safe producer-consumer model.<br />
/// 使用 Channel&lt;T&gt; 提供背压安全的生产者-消费者模型。
/// </summary>
public sealed class WorkspaceMessageBus : IMessageBus
{
    /// <summary>Drain queue storage / Drain 队列存储</summary>
    private readonly ConcurrentDictionary<string, Channel<BusEntry>> _queues = new();

    /// <summary>Replay log storage / Replay 日志存储</summary>
    private readonly ConcurrentDictionary<string, List<LogEntry>> _logs = new();

    /// <summary>Pub/Sub subscriber storage / Pub/Sub 订阅者存储</summary>
    private readonly ConcurrentDictionary<string, List<Func<BusEntry, Task>>> _subscribers = new();

    /// <summary>Synchronization guard for log operations / 日志操作同步锁</summary>
    private readonly object _logLock = new();

    // ── Mode A: Drain queue (单一消费者模式) ──

    /// <inheritdoc />
    public ValueTask QueuePushAsync(string queue, BusEntry entry, CancellationToken ct = default)
    {
        // 获取或创建无界通道，然后写入条目
        var ch = _queues.GetOrAdd(queue, _ => Channel.CreateUnbounded<BusEntry>());
        return ch.Writer.WriteAsync(entry, ct);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<BusEntry> QueueDrainAsync(string queue, CancellationToken ct = default)
    {
        // 获取或创建无界通道，异步读取所有条目
        var ch = _queues.GetOrAdd(queue, _ => Channel.CreateUnbounded<BusEntry>());
        return ch.Reader.ReadAllAsync(ct);
    }

    /// <inheritdoc />
    public ValueTask QueueDeleteAsync(string queue, string entryId) =>
        ValueTask.CompletedTask; // Channel<T> does not support random deletion / Channel<T> 不支持随机删除

    // ── Mode C: Replay log (多消费者日志模式) ──

    /// <inheritdoc />
    public ValueTask LogAppendAsync(string log, BusEntry entry, CancellationToken ct = default)
    {
        lock (_logLock)
        {
            // 获取或创建日志条目列表，分配序列号后追加
            var list = _logs.GetOrAdd(log, _ => []);
            var seq = list.Count;
            list.Add(new LogEntry(seq, entry));
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<BusEntry> LogReadAsync(string log, long startSeq, CancellationToken ct = default)
    {
        return ReadLogEntriesAsync(log, startSeq, ct);
    }

    /// <summary>
    /// Internal async enumerable for reading log entries.<br />
    /// 内部异步枚举器，用于读取日志条目。
    /// </summary>
    private async IAsyncEnumerable<BusEntry> ReadLogEntriesAsync(string log, long startSeq,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // 在锁内获取日志快照，避免长时间持有锁
        List<LogEntry> snapshot;
        lock (_logLock)
        {
            if (!_logs.TryGetValue(log, out var list))
                yield break;
            snapshot = [.. list];
        }

        // 跳过起始序列号之前的条目，依次返回
        foreach (var entry in snapshot.Skip((int)startSeq))
        {
            ct.ThrowIfCancellationRequested();
            yield return entry.Entry with { Sequence = entry.Sequence };
        }
    }

    /// <inheritdoc />
    public ValueTask LogTrimAsync(string log, long upToSeq)
    {
        lock (_logLock)
        {
            if (_logs.TryGetValue(log, out var list))
            {
                // 若裁剪位置超过列表长度则直接删除整个日志
                if (upToSeq >= list.Count)
                    _logs.TryRemove(log, out _);
                else
                    list.RemoveRange(0, (int)upToSeq);
            }
        }
        return ValueTask.CompletedTask;
    }

    // ── Mode D: Pub/Sub (发布/订阅广播模式) ──

    /// <inheritdoc />
    public ValueTask PublishAsync(string topic, BusEntry entry, CancellationToken ct = default)
    {
        // 遍历所有订阅者，fire-and-forget 调用处理函数
        if (_subscribers.TryGetValue(topic, out var handlers))
        {
            foreach (var handler in handlers)
                _ = handler(entry);
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public IDisposable Subscribe(string topic, Func<BusEntry, Task> handler)
    {
        // 将处理函数添加到订阅者列表
        _subscribers.AddOrUpdate(topic,
            _ => [handler],
            (_, list) => { list.Add(handler); return list; });
        return new Subscription(() => Unsubscribe(topic, handler));
    }

    /// <summary>
    /// Remove a handler from a topic's subscriber list.<br />
    /// 从主题的订阅者列表中移除指定处理函数。
    /// </summary>
    private void Unsubscribe(string topic, Func<BusEntry, Task> handler)
    {
        if (_subscribers.TryGetValue(topic, out var list))
        {
            list.Remove(handler);
            if (list.Count == 0)
                _subscribers.TryRemove(topic, out _);
        }
    }

    // ── Inbox (代理收件箱，基于 Drain queue 实现) ──

    /// <inheritdoc />
    public ValueTask InboxPushAsync(string agentId, BusEntry entry, CancellationToken ct = default)
        => QueuePushAsync($"inbox:{agentId}", entry, ct);

    /// <inheritdoc />
    public IAsyncEnumerable<BusEntry> InboxDrainAsync(string agentId, CancellationToken ct = default)
        => QueueDrainAsync($"inbox:{agentId}", ct);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // 清理所有资源
        _queues.Clear();
        _logs.Clear();
        _subscribers.Clear();
    }

    /// <summary>
    /// Internal log entry record with a sequence number.<br />
    /// 内部日志条目记录，包含序列号。
    /// </summary>
    private sealed record LogEntry(long Sequence, BusEntry Entry);

    /// <summary>
    /// Subscription disposable that triggers unsubscription on dispose.<br />
    /// 订阅的 disposable 封装，释放时触发取消订阅。
    /// </summary>
    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        public void Dispose() => unsubscribe();
    }
}
