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
/// 会话轮次门。确保同一会话的调用串行执行。对标 Java SessionTurnGate。
/// </summary>
public sealed class SessionTurnGate
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

    /// <summary>
    /// 获取会话的串行执行许可。同一会话同时只能有一个请求获得许可。
    /// Acquire a serial execution permit for a session. Only one request per session can acquire at a time.
    /// </summary>
    /// <param name="sessionId">会话 ID / The session ID.</param>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    /// <returns>释放许可的 IDisposable / A disposable lease that releases the permit.</returns>
    public async Task<IDisposable> AcquireAsync(string sessionId, CancellationToken ct = default)
    {
        var sem = _gates.GetOrAdd(sessionId, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct);
        return new TurnLease(sem);
    }

    /// <summary>
    /// 检查指定会话当前是否正在执行中。
    /// Check whether the specified session is currently executing.
    /// </summary>
    /// <param name="sessionId">会话 ID / The session ID.</param>
    /// <returns>正在执行返回 true / True if running.</returns>
    public bool IsRunning(string sessionId) =>
        _gates.TryGetValue(sessionId, out var sem) && sem.CurrentCount == 0;

    private sealed class TurnLease(SemaphoreSlim sem) : IDisposable
    {
        public void Dispose() => sem.Release();
    }
}
