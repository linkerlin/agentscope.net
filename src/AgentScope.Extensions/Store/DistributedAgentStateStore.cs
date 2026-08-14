// Copyright 2024-2026 the original author or authors.
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

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.State;

namespace AgentScope.Extensions.Store;

/// <summary>
/// 基于 IDistributedStore 的 Agent 状态存储抽象基类。
/// 把 AgentState 以 JSON 形式存入任意分布式 KV 存储，并支持版本化乐观并发（CAS）。
/// 子类只需注入对应的 *DistributedStore 即可获得持久化状态存储能力。
/// 对应 Java: 各 *AgentStateStore（Redis/MySql/Postgres/Oss/Cos）的公共基类行为。
/// </summary>
public abstract class DistributedAgentStateStore : IAgentStateStore
{
    private readonly IDistributedStore _store;
    private readonly string _keyPrefix;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected DistributedAgentStateStore(IDistributedStore store, string keyPrefix = "agentstate")
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _keyPrefix = keyPrefix;
    }

    /// <inheritdoc />
    public bool SupportsVersioning => true;

    private string Key(string userId, string sessionId, string key) =>
        $"{_keyPrefix}:{userId ?? ""}:{sessionId}:{key}";

    private sealed class Entry
    {
        public long Version { get; set; }
        public AgentState? State { get; set; }
    }

    /// <inheritdoc />
    public async Task<AgentState?> GetAsync(string userId, string sessionId, string key)
    {
        var entry = await ReadAsync(userId, sessionId, key).ConfigureAwait(false);
        return entry?.State;
    }

    /// <inheritdoc />
    public async Task<VersionedState<AgentState>?> GetVersionedAsync(string userId, string sessionId, string key)
    {
        var entry = await ReadAsync(userId, sessionId, key).ConfigureAwait(false);
        if (entry?.State == null) return null;
        return new VersionedState<AgentState>(entry.Version, entry.State);
    }

    /// <inheritdoc />
    public async Task SaveAsync(string userId, string sessionId, string key, AgentState state)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var existing = await ReadAsync(userId, sessionId, key).ConfigureAwait(false);
            var version = (existing?.Version ?? 0) + 1;
            await WriteAsync(userId, sessionId, key, version, state).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<long> SaveIfVersionAsync(string userId, string sessionId, string key, AgentState state, long expectedVersion)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            var existing = await ReadAsync(userId, sessionId, key).ConfigureAwait(false);

            if (expectedVersion == IAgentStateStore.Unversioned)
            {
                if (existing?.State != null)
                {
                    throw new ConcurrentSessionModificationException(
                        $"状态已存在，CAS 写入失败（key={key}）。");
                }

                await WriteAsync(userId, sessionId, key, 1, state).ConfigureAwait(false);
                return 1;
            }

            if (existing == null || existing.Version != expectedVersion)
            {
                throw new ConcurrentSessionModificationException(
                    $"版本不匹配，CAS 写入失败（key={key}, 期望={expectedVersion}, 实际={existing?.Version}）。");
            }

            var next = expectedVersion + 1;
            await WriteAsync(userId, sessionId, key, next, state).ConfigureAwait(false);
            return next;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Entry?> ReadAsync(string userId, string sessionId, string key)
    {
        var json = await _store.GetAsync(Key(userId, sessionId, key)).ConfigureAwait(false);
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<Entry>(json!, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private async Task WriteAsync(string userId, string sessionId, string key, long version, AgentState state)
    {
        var entry = new Entry { Version = version, State = state };
        var json = JsonSerializer.Serialize(entry, JsonOptions);
        await _store.SetAsync(Key(userId, sessionId, key), json).ConfigureAwait(false);
    }
}
