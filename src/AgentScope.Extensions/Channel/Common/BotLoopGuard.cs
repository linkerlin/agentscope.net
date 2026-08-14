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
/// 按对等体（peer）做滑动窗口限流，防止机器人被卷入与另一个机器人（或卡住用户/脚本）的失控交互。
/// 对应 Java: io.agentscope.extensions.channel.common.BotLoopGuard
/// </summary>
/// <remarks>默认 20 事件 / 60 秒窗口；超限后进入 60 秒冷却，期间 <see cref="Allow"/> 返回 false。</remarks>
public sealed class BotLoopGuard
{
    private readonly int _maxEventsPerWindow;
    private readonly long _windowMillis;
    private readonly long _cooldownMillis;
    private readonly ConcurrentDictionary<string, PeerState> _states = new();

    public BotLoopGuard() : this(20, 60_000L, 60_000L) { }

    public BotLoopGuard(int maxEventsPerWindow, long windowMillis, long cooldownMillis)
    {
        if (maxEventsPerWindow <= 0 || windowMillis <= 0 || cooldownMillis <= 0)
            throw new ArgumentException("all bounds must be positive");
        _maxEventsPerWindow = maxEventsPerWindow;
        _windowMillis = windowMillis;
        _cooldownMillis = cooldownMillis;
    }

    /// <summary>记录一次事件并判断该 peer 是否在预算内；超限进入冷却并返回 false。</summary>
    public bool Allow(string? peerKey)
    {
        if (string.IsNullOrWhiteSpace(peerKey))
            return true;

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var state = _states.GetOrAdd(peerKey, _ => new PeerState());
        lock (state)
        {
            if (state.CooldownUntilMs > now)
                return false;

            while (state.Events.Count > 0 && now - state.Events.Peek() > _windowMillis)
                state.Events.Dequeue();

            if (state.Events.Count >= _maxEventsPerWindow)
            {
                state.CooldownUntilMs = now + _cooldownMillis;
                state.Events.Clear();
                return false;
            }

            state.Events.Enqueue(now);
            return true;
        }
    }

    /// <summary>返回该 peer 当前是否处于冷却期。</summary>
    public bool IsCoolingDown(string? peerKey)
    {
        if (peerKey == null) return false;
        if (!_states.TryGetValue(peerKey, out var state)) return false;
        lock (state)
            return state.CooldownUntilMs > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private sealed class PeerState
    {
        public readonly Queue<long> Events = new();
        public long CooldownUntilMs;
    }
}
