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

namespace AgentScope.Core.State;

/// <summary>
/// In-memory agent state store implementation backed by a concurrent dictionary, supporting versioned optimistic concurrency (CAS).
/// 基于内存并发字典的 Agent 状态存储实现，支持版本化乐观并发（CAS）。
/// Corresponds to Java: io.agentscope.core.state.InMemoryAgentStateStore
/// 对应 Java: io.agentscope.core.state.InMemoryAgentStateStore
/// </summary>
public class InMemoryAgentStateStore : IAgentStateStore
{
    /// <summary>
    /// Internal concurrent dictionary storing versioned states.
    /// 存储版本化状态的内部并发字典。
    /// </summary>
    private readonly ConcurrentDictionary<string, VersionedState<AgentState>> _store = new();

    /// <summary>
    /// Lock object for CAS serialization.
    /// CAS 序列化用的锁对象。
    /// </summary>
    private readonly object _casLock = new();

    /// <inheritdoc />
    public bool SupportsVersioning => true;

    /// <summary>
    /// Builds a composite key from user ID, session ID, and state key.
    /// 从用户 ID、会话 ID 和状态键构造复合键。
    /// </summary>
    private static string Key(string userId, string sessionId, string key) =>
        $"{userId ?? ""}::{sessionId}::{key}";

    /// <inheritdoc />
    public Task<AgentState?> GetAsync(string userId, string sessionId, string key)
    {
        _store.TryGetValue(Key(userId, sessionId, key), out var v);
        return Task.FromResult(v?.State);
    }

    /// <inheritdoc />
    public Task<VersionedState<AgentState>?> GetVersionedAsync(string userId, string sessionId, string key)
    {
        _store.TryGetValue(Key(userId, sessionId, key), out var v);
        return Task.FromResult(v);
    }

    /// <inheritdoc />
    public Task SaveAsync(string userId, string sessionId, string key, AgentState state)
    {
        var k = Key(userId, sessionId, key);
        lock (_casLock)
        {
            // Increment version if exists, otherwise start at 1
            // 如果存在则递增版本，否则从 1 开始
            var existing = _store.TryGetValue(k, out var cur) ? cur.Version + 1 : 1L;
            _store[k] = new VersionedState<AgentState>(existing, state);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> SaveIfVersionAsync(string userId, string sessionId, string key, AgentState state, long expectedVersion)
    {
        var k = Key(userId, sessionId, key);

        // Place the entire CAS (validate + write) in one critical section to guarantee versioning semantics
        // 整个 CAS（校验+写入）置于同一临界区，保证版本化承诺
        lock (_casLock)
        {
            // First write: expectedVersion == Unversioned(0)
            // 首次写入：expectedVersion == Unversioned(0)
            if (expectedVersion == IAgentStateStore.Unversioned)
            {
                if (_store.TryGetValue(k, out _))
                {
                    throw new ConcurrentSessionModificationException(
                        $"状态已存在，CAS 写入失败（key={key}）。");
                }

                _store[k] = new VersionedState<AgentState>(1, state);
                return Task.FromResult(1L);
            }

            // Version matches: replace
            // 版本匹配替换
            if (_store.TryGetValue(k, out var current) && current.Version == expectedVersion)
            {
                var next = new VersionedState<AgentState>(expectedVersion + 1, state);
                _store[k] = next;
                return Task.FromResult(next.Version);
            }

            throw new ConcurrentSessionModificationException(
                $"版本不匹配，CAS 写入失败（key={key}, 期望={expectedVersion}）。");
        }
    }
}
