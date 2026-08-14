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
/// 基于内存并发字典的 Agent 状态存储实现，支持版本化乐观并发（CAS）。
/// 对应 Java: io.agentscope.core.state.InMemoryAgentStateStore
/// </summary>
public class InMemoryAgentStateStore : IAgentStateStore
{
    private readonly ConcurrentDictionary<string, VersionedState<AgentState>> _store = new();
    private readonly object _casLock = new();

    /// <inheritdoc />
    public bool SupportsVersioning => true;

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
            var existing = _store.TryGetValue(k, out var cur) ? cur.Version + 1 : 1L;
            _store[k] = new VersionedState<AgentState>(existing, state);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> SaveIfVersionAsync(string userId, string sessionId, string key, AgentState state, long expectedVersion)
    {
        var k = Key(userId, sessionId, key);

        // 整个 CAS（校验+写入）置于同一临界区，保证版本化承诺
        lock (_casLock)
        {
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
