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
/// Sliding-window rate limiter per peer, preventing a bot from being caught in
/// an uncontrolled interaction loop with another bot (or a stuck user/script).
/// Maps to Java: io.agentscope.extensions.channel.common.BotLoopGuard
/// 按对等体（peer）做滑动窗口限流，防止机器人被卷入与另一个机器人（或卡住用户/脚本）的失控交互。
/// 对应 Java: io.agentscope.extensions.channel.common.BotLoopGuard
/// </summary>
/// <remarks>
/// Default: 20 events / 60s window; after exceeding, enters 60s cooldown during which <see cref="Allow"/> returns false.
/// 默认 20 事件 / 60 秒窗口；超限后进入 60 秒冷却，期间 <see cref="Allow"/> 返回 false。
/// </remarks>
public sealed class BotLoopGuard
{
    private readonly int _maxEventsPerWindow;
    private readonly long _windowMillis;
    private readonly long _cooldownMillis;
    private readonly ConcurrentDictionary<string, PeerState> _states = new();

    /// <summary>
    /// Creates a guard with default settings: 20 events per 60s window, 60s cooldown.
    /// 使用默认设置创建防护：每个 60 秒窗口 20 个事件，60 秒冷却。
    /// </summary>
    public BotLoopGuard() : this(20, 60_000L, 60_000L) { }

    /// <summary>
    /// Creates a guard with custom bounds.
    /// 使用自定义边界创建防护。
    /// </summary>
    /// <param name="maxEventsPerWindow">Maximum events per sliding window. 每个滑动窗口的最大事件数。</param>
    /// <param name="windowMillis">Sliding window duration in milliseconds. 滑动窗口时长（毫秒）。</param>
    /// <param name="cooldownMillis">Cooldown duration in milliseconds after rate limit is hit. 触发限流后的冷却时长（毫秒）。</param>
    /// <exception cref="ArgumentException">Thrown when any bound is not positive. 当任何边界不是正数时抛出。</exception>
    public BotLoopGuard(int maxEventsPerWindow, long windowMillis, long cooldownMillis)
    {
        if (maxEventsPerWindow <= 0 || windowMillis <= 0 || cooldownMillis <= 0)
            throw new ArgumentException("all bounds must be positive");
        _maxEventsPerWindow = maxEventsPerWindow;
        _windowMillis = windowMillis;
        _cooldownMillis = cooldownMillis;
    }

    /// <summary>
    /// Records one event and determines whether the given peer is within budget;
    /// returns false and enters cooldown if the limit is exceeded.
    /// 记录一次事件并判断该 peer 是否在预算内；超限进入冷却并返回 false。
    /// </summary>
    /// <param name="peerKey">The peer identifier. Null or empty allows all traffic. 对等体标识。null 或空字符串允许所有流量。</param>
    /// <returns>True if within budget; false if rate-limited or in cooldown. 在预算内返回 true；被限流或在冷却中返回 false。</returns>
    public bool Allow(string? peerKey)
    {
        // Allow all traffic if no peer key is provided
        // 如果没有提供 peer key，允许所有流量
        if (string.IsNullOrWhiteSpace(peerKey))
            return true;

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var state = _states.GetOrAdd(peerKey, _ => new PeerState());
        lock (state)
        {
            // Check if currently in cooldown
            // 检查当前是否在冷却中
            if (state.CooldownUntilMs > now)
                return false;

            // Remove events outside the sliding window
            // 移除滑动窗口之外的事件
            while (state.Events.Count > 0 && now - state.Events.Peek() > _windowMillis)
                state.Events.Dequeue();

            // If limit is exceeded, enter cooldown
            // 如果超出限制，进入冷却
            if (state.Events.Count >= _maxEventsPerWindow)
            {
                state.CooldownUntilMs = now + _cooldownMillis;
                state.Events.Clear();
                return false;
            }

            // Record the event timestamp
            // 记录事件时间戳
            state.Events.Enqueue(now);
            return true;
        }
    }

    /// <summary>
    /// Returns whether the given peer is currently in cooldown.
    /// 返回该 peer 当前是否处于冷却期。
    /// </summary>
    /// <param name="peerKey">The peer identifier. 对等体标识。</param>
    /// <returns>True if in cooldown. 如果在冷却中返回 true。</returns>
    public bool IsCoolingDown(string? peerKey)
    {
        if (peerKey == null) return false;
        if (!_states.TryGetValue(peerKey, out var state)) return false;
        lock (state)
            return state.CooldownUntilMs > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Internal state for each peer tracking events and cooldown.
    /// 每个 peer 的内部状态，追踪事件和冷却。
    /// </summary>
    private sealed class PeerState
    {
        /// <summary>Queue of event timestamps in milliseconds. 事件时间戳队列（毫秒）。</summary>
        public readonly Queue<long> Events = new();
        /// <summary>Cooldown end timestamp in milliseconds, or 0 if not in cooldown. 冷却结束时间戳（毫秒），0 表示不在冷却中。</summary>
        public long CooldownUntilMs;
    }
}
