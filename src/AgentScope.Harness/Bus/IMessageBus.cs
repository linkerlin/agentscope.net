namespace AgentScope.Harness.Bus;

/// <summary>
/// 消息总线接口。对标 Java MessageBus。
/// 三种消费模式：Drain queue (A)、Replay log (C)、Pub/Sub (D)。
/// 使用 Channel&lt;T&gt; 实现背压安全（替代 Java Reactor Flux）。
/// </summary>
public interface IMessageBus : IAsyncDisposable
{
    // Mode A: Drain queue (单一消费者, ack-on-read)
    ValueTask QueuePushAsync(string queue, BusEntry entry, CancellationToken ct = default);
    IAsyncEnumerable<BusEntry> QueueDrainAsync(string queue, CancellationToken ct = default);
    ValueTask QueueDeleteAsync(string queue, string entryId);

    // Mode C: Replay log (多消费者)
    ValueTask LogAppendAsync(string log, BusEntry entry, CancellationToken ct = default);
    IAsyncEnumerable<BusEntry> LogReadAsync(string log, long startSeq, CancellationToken ct = default);
    ValueTask LogTrimAsync(string log, long upToSeq);

    // Mode D: Pub/Sub (广播)
    ValueTask PublishAsync(string topic, BusEntry entry, CancellationToken ct = default);
    IDisposable Subscribe(string topic, Func<BusEntry, Task> handler);

    // 领域辅助（收件箱）
    ValueTask InboxPushAsync(string agentId, BusEntry entry, CancellationToken ct = default);
    IAsyncEnumerable<BusEntry> InboxDrainAsync(string agentId, CancellationToken ct = default);
}

/// <summary>
/// 总线条目。对标 Java BusEntry。
/// </summary>
public readonly record struct BusEntry(string Id, string Key, object Payload)
{
    public long Sequence { get; init; }
}
