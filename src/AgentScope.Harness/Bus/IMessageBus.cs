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

namespace AgentScope.Harness.Bus;

/// <summary>
/// Message bus interface, corresponding to Java MessageBus.<br />
/// 消息总线接口，对标 Java MessageBus。<br />
/// Three consumption modes: Drain queue (A), Replay log (C), Pub/Sub (D).<br />
/// 三种消费模式：Drain queue (A)、Replay log (C)、Pub/Sub (D)。<br />
/// Uses <see cref="System.Threading.Channels.Channel{T}"/> for backpressure safety (replacing Java Reactor Flux).<br />
/// 使用 Channel&lt;T&gt; 实现背压安全（替代 Java Reactor Flux）。
/// </summary>
public interface IMessageBus : IAsyncDisposable
{
    // ── Mode A: Drain queue (单一消费者, ack-on-read) ──

    /// <summary>
    /// Push an entry onto a drain queue.<br />
    /// 将条目推入 drain 队列（单一消费者模式）。
    /// </summary>
    /// <param name="queue">Queue name / 队列名称</param>
    /// <param name="entry">The bus entry / 总线条目</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    ValueTask QueuePushAsync(string queue, BusEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Asynchronously drain all entries from the queue.<br />
    /// 异步排空队列中的所有条目。
    /// </summary>
    /// <param name="queue">Queue name / 队列名称</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>An async sequence of bus entries / 总线条目的异步序列</returns>
    IAsyncEnumerable<BusEntry> QueueDrainAsync(string queue, CancellationToken ct = default);

    /// <summary>
    /// Delete a specific entry from the queue by ID.<br />
    /// 根据 ID 从队列中删除指定条目。
    /// </summary>
    /// <param name="queue">Queue name / 队列名称</param>
    /// <param name="entryId">Entry identifier / 条目标识符</param>
    ValueTask QueueDeleteAsync(string queue, string entryId);

    // ── Mode C: Replay log (多消费者) ──

    /// <summary>
    /// Append an entry to a replay log.<br />
    /// 将条目追加到 replay 日志中。
    /// </summary>
    /// <param name="log">Log name / 日志名称</param>
    /// <param name="entry">The bus entry / 总线条目</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    ValueTask LogAppendAsync(string log, BusEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Read entries from a replay log starting at the given sequence number.<br />
    /// 从指定序列号开始读取 replay 日志中的条目。
    /// </summary>
    /// <param name="log">Log name / 日志名称</param>
    /// <param name="startSeq">Starting sequence number (inclusive) / 起始序列号（包含）</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>An async sequence of bus entries / 总线条目的异步序列</returns>
    IAsyncEnumerable<BusEntry> LogReadAsync(string log, long startSeq, CancellationToken ct = default);

    /// <summary>
    /// Trim the replay log up to (and including) the given sequence number.<br />
    /// 裁剪 replay 日志至指定序列号（包含该序列号）。
    /// </summary>
    /// <param name="log">Log name / 日志名称</param>
    /// <param name="upToSeq">Sequence number to trim up to / 裁剪到的序列号</param>
    ValueTask LogTrimAsync(string log, long upToSeq);

    // ── Mode D: Pub/Sub (广播) ──

    /// <summary>
    /// Publish an entry to all subscribers of a topic.<br />
    /// 向主题的所有订阅者发布条目。
    /// </summary>
    /// <param name="topic">Topic name / 主题名称</param>
    /// <param name="entry">The bus entry / 总线条目</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    ValueTask PublishAsync(string topic, BusEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Subscribe to a topic with the given handler.<br />
    /// 使用指定处理函数订阅主题。
    /// </summary>
    /// <param name="topic">Topic name / 主题名称</param>
    /// <param name="handler">Callback invoked for each published entry / 每有一条发布消息时调用的回调</param>
    /// <returns>An <see cref="IDisposable"/> to unsubscribe / 用于取消订阅的 <see cref="IDisposable"/></returns>
    IDisposable Subscribe(string topic, Func<BusEntry, Task> handler);

    // ── Domain helpers: Inbox (领域辅助：收件箱) ──

    /// <summary>
    /// Push an entry into an agent's inbox.<br />
    /// 将条目推入代理的收件箱。
    /// </summary>
    /// <param name="agentId">Agent identifier / 代理标识符</param>
    /// <param name="entry">The bus entry / 总线条目</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    ValueTask InboxPushAsync(string agentId, BusEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Drain all entries from an agent's inbox.<br />
    /// 排空代理收件箱中的所有条目。
    /// </summary>
    /// <param name="agentId">Agent identifier / 代理标识符</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>An async sequence of bus entries / 总线条目的异步序列</returns>
    IAsyncEnumerable<BusEntry> InboxDrainAsync(string agentId, CancellationToken ct = default);
}

/// <summary>
/// Bus entry record, corresponding to Java BusEntry.<br />
/// 总线条目记录结构，对标 Java BusEntry。
/// </summary>
/// <param name="Id">Unique entry identifier / 条目唯一标识符</param>
/// <param name="Key">Entry key / 条目键</param>
/// <param name="Payload">Entry payload / 条目负载</param>
public readonly record struct BusEntry(string Id, string Key, object Payload)
{
    /// <summary>
    /// Monotonically increasing sequence number within the log.<br />
    /// 日志内单调递增的序列号。
    /// </summary>
    public long Sequence { get; init; }
}
