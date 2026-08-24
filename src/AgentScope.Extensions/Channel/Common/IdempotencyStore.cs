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

namespace AgentScope.Extensions.Channel.Common;

/// <summary>
/// Bounded per-channel idempotency store for deduplicating inbound webhook events
/// (e.g. WeCom retrying the same msgId on failure).
/// Maps to Java: io.agentscope.extensions.channel.common.IdempotencyStore
/// 有界的按渠道幂等存储，用于去重入站 webhook 事件（如 WeCom 失败重试同一 msgId）。
/// 对应 Java: io.agentscope.extensions.channel.common.IdempotencyStore
/// </summary>
/// <remarks>
/// Internal map has a capacity limit; when full, the oldest entries are evicted by insertion order,
/// and entries are lazily expired after TTL.
/// 内部映射有上限；满时按插入序淘汰最旧条目，并在 TTL 后惰性过期。
/// </remarks>
public sealed class IdempotencyStore
{
    private readonly long _ttlMillis;
    private readonly int _maxEntries;
    private readonly ConcurrentDictionary<string, long> _seen = new();

    /// <summary>
    /// Creates a store with default settings: 5 minutes TTL, 10,000 max entries.
    /// 使用默认设置创建存储：5 分钟 TTL，1 万条上限。
    /// </summary>
    public IdempotencyStore() : this(5 * 60_000L, 10_000) { }

    /// <summary>
    /// Creates a store with custom TTL and capacity.
    /// 使用自定义 TTL 和容量创建存储。
    /// </summary>
    /// <param name="ttlMillis">Time-to-live in milliseconds for each seen key. 每个已见 key 的存活时间（毫秒）。</param>
    /// <param name="maxEntries">Maximum number of entries in the store. 存储中的最大条目数。</param>
    /// <exception cref="ArgumentException">Thrown when any parameter is not positive. 当任何参数不是正数时抛出。</exception>
    public IdempotencyStore(long ttlMillis, int maxEntries)
    {
        if (ttlMillis <= 0) throw new ArgumentException("ttlMillis must be positive");
        if (maxEntries <= 0) throw new ArgumentException("maxEntries must be positive");
        _ttlMillis = ttlMillis;
        _maxEntries = maxEntries;
    }

    /// <summary>
    /// Records a key; returns true on first sight (caller should proceed),
    /// or false if already seen within the TTL window.
    /// 记录 key；首次见到返回 true（调用方应继续），TTL 内已见过返回 false。
    /// </summary>
    /// <param name="key">The idempotency key (e.g. message ID). Null is always treated as first-seen. 幂等 key（如消息 ID）。null 始终被视为首次见到。</param>
    /// <returns>True if this is the first occurrence within TTL; false if duplicate. 在 TTL 内首次出现返回 true；重复返回 false。</returns>
    public bool FirstSeen(string? key)
    {
        if (key == null) return true;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Sweep(now);
        if (_seen.TryAdd(key, now))
            return true;
        // Already exists: check if TTL has expired
        // 已存在：判断是否已过 TTL
        if (_seen.TryGetValue(key, out var prior))
            return now - prior > _ttlMillis;
        return true;
    }

    /// <summary>
    /// Evicts expired entries and enforces the hard capacity limit.
    /// 逐出过期条目并强制执行硬容量上限。
    /// </summary>
    private void Sweep(long now)
    {
        if (_seen.Count < _maxEntries)
            return;

        // Lazy eviction: remove expired entries
        // 惰性淘汰：移除已过期的条目
        foreach (var e in _seen)
        {
            if (now - e.Value > _ttlMillis)
                _seen.TryRemove(e.Key, out _);
        }

        // Hard capacity: evict arbitrary entries when still over budget
        // 硬上限：仍超预算时淘汰任意条目
        while (_seen.Count >= _maxEntries)
        {
            var key = _seen.Keys.FirstOrDefault();
            if (key == null) break;
            _seen.TryRemove(key, out _);
        }
    }

    /// <summary>Gets the current number of tracked entries. 获取当前追踪的条目数。</summary>
    public int Count => _seen.Count;
}
