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

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 唤醒调度器：当外部事件（消息/定时器/异步完成）到达时，唤醒对应的会话/Agent 继续处理。
/// 对应 Java: io.agentscope.harness.agent.gateway.WakeupDispatcher
/// </summary>
public sealed class WakeupDispatcher
{
    private readonly ConcurrentDictionary<string, List<WakeupSubscription>> _subscribers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 订阅某个唤醒键（如会话 ID），当该键被唤醒时执行回调。
    /// Subscribe to a wakeup key (e.g., session ID); the callback executes when the key is woken.
    /// </summary>
    /// <param name="key">唤醒键 / The wakeup key.</param>
    /// <param name="onWakeup">唤醒回调 / The wakeup callback.</param>
    /// <returns>可用于取消订阅的 IDisposable / A disposable to unsubscribe.</returns>
    /// <exception cref="ArgumentException">key 为空或空白时抛出 / Thrown when key is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">onWakeup 为 null 时抛出 / Thrown when onWakeup is null.</exception>
    public IDisposable Subscribe(string key, Action<string> onWakeup)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("key 必填", nameof(key));
        if (onWakeup == null) throw new ArgumentNullException(nameof(onWakeup));

        var sub = new WakeupSubscription(onWakeup);
        var list = _subscribers.GetOrAdd(key, _ => new List<WakeupSubscription>());
        lock (list)
        {
            list.Add(sub);
        }

        sub.OnUnsubscribe = () =>
        {
            lock (list)
            {
                list.Remove(sub);
            }
        };

        return sub;
    }

    /// <summary>
    /// 唤醒某个键的所有订阅者。
    /// Wake all subscribers of the specified key.
    /// </summary>
    /// <param name="key">唤醒键 / The wakeup key.</param>
    /// <param name="payload">可选载荷 / Optional payload.</param>
    public void Wakeup(string key, string? payload = null)
    {
        if (!_subscribers.TryGetValue(key ?? "", out var list)) return;
        List<WakeupSubscription> snapshot;
        lock (list)
        {
            snapshot = new List<WakeupSubscription>(list);
        }

        foreach (var sub in snapshot)
        {
            try
            {
                sub.Callback(payload ?? key);
            }
            catch
            {
                // 单个订阅者异常不影响其它
            }
        }
    }

    private sealed class WakeupSubscription : IDisposable
    {
        public Action<string> Callback { get; }
        public Action? OnUnsubscribe { get; set; }

        public WakeupSubscription(Action<string> callback) => Callback = callback;

        public void Dispose() => OnUnsubscribe?.Invoke();
    }
}
