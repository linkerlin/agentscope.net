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
/// 有界的按渠道幂等存储，用于去重入站 webhook 事件（如 WeCom 失败重试同一 msgId）。
/// 对应 Java: io.agentscope.extensions.channel.common.IdempotencyStore
/// </summary>
/// <remarks>内部映射有上限；满时按插入序淘汰最旧条目，并在 TTL 后惰性过期。</remarks>
public sealed class IdempotencyStore
{
    private readonly long _ttlMillis;
    private readonly int _maxEntries;
    private readonly ConcurrentDictionary<string, long> _seen = new();

    /// <summary>默认 5 分钟 TTL、1 万条上限。</summary>
    public IdempotencyStore() : this(5 * 60_000L, 10_000) { }

    public IdempotencyStore(long ttlMillis, int maxEntries)
    {
        if (ttlMillis <= 0) throw new ArgumentException("ttlMillis must be positive");
        if (maxEntries <= 0) throw new ArgumentException("maxEntries must be positive");
        _ttlMillis = ttlMillis;
        _maxEntries = maxEntries;
    }

    /// <summary>记录 key；首次见到返回 true（调用方应继续），TTL 内已见过返回 false。</summary>
    public bool FirstSeen(string? key)
    {
        if (key == null) return true;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Sweep(now);
        if (_seen.TryAdd(key, now))
            return true;
        // 已存在：判断是否已过 TTL
        if (_seen.TryGetValue(key, out var prior))
            return now - prior > _ttlMillis;
        return true;
    }

    private void Sweep(long now)
    {
        if (_seen.Count < _maxEntries)
            return;

        foreach (var e in _seen)
        {
            if (now - e.Value > _ttlMillis)
                _seen.TryRemove(e.Key, out _);
        }

        // 硬上限：仍超预算时淘汰任意条目
        while (_seen.Count >= _maxEntries)
        {
            var key = _seen.Keys.FirstOrDefault();
            if (key == null) break;
            _seen.TryRemove(key, out _);
        }
    }

    public int Count => _seen.Count;
}
