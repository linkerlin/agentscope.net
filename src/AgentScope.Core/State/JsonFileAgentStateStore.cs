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
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgentScope.Core.State;

/// <summary>
/// 基于 JSON 文件持久化的 Agent 状态存储实现。
/// 内存中维护版本化状态，并在写入后同步落盘到单个 JSON 文件。
/// 对应 Java: io.agentscope.core.state.JsonFileAgentStateStore
/// </summary>
public class JsonFileAgentStateStore : IAgentStateStore
{
    private readonly ConcurrentDictionary<string, VersionedState<AgentState>> _store = new();
    private readonly string _filePath;
    private readonly object _fileLock = new();
    private readonly object _opLock = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = false };

    /// <inheritdoc />
    public bool SupportsVersioning => true;

    /// <summary>
    /// 创建 JSON 文件状态存储。
    /// </summary>
    /// <param name="filePath">持久化文件路径；不存在则首次写入时创建。</param>
    public JsonFileAgentStateStore(string filePath)
    {
        _filePath = filePath ?? throw new System.ArgumentNullException(nameof(filePath));
        Load();
    }

    private static string Key(string userId, string sessionId, string key) =>
        $"{userId ?? ""}::{sessionId}::{key}";

    /// <inheritdoc />
    public Task<AgentState?> GetAsync(string userId, string sessionId, string key)
    {
        lock (_opLock)
        {
            _store.TryGetValue(Key(userId, sessionId, key), out var v);
            return Task.FromResult(v?.State);
        }
    }

    /// <inheritdoc />
    public Task<VersionedState<AgentState>?> GetVersionedAsync(string userId, string sessionId, string key)
    {
        lock (_opLock)
        {
            _store.TryGetValue(Key(userId, sessionId, key), out var v);
            return Task.FromResult(v);
        }
    }

    /// <inheritdoc />
    public Task SaveAsync(string userId, string sessionId, string key, AgentState state)
    {
        lock (_opLock)
        {
            var k = Key(userId, sessionId, key);
            var version = _store.TryGetValue(k, out var cur) ? cur.Version + 1 : 1L;
            _store[k] = new VersionedState<AgentState>(version, state);
            Persist();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<long> SaveIfVersionAsync(string userId, string sessionId, string key, AgentState state, long expectedVersion)
    {
        // 整个 CAS（校验+更新+落盘）置于同一临界区，保证版本化承诺
        lock (_opLock)
        {
            var k = Key(userId, sessionId, key);

            if (expectedVersion == IAgentStateStore.Unversioned)
            {
                if (_store.TryGetValue(k, out _))
                {
                    throw new ConcurrentSessionModificationException(
                        $"状态已存在，CAS 写入失败（key={key}）。");
                }

                _store[k] = new VersionedState<AgentState>(1, state);
                Persist();
                return Task.FromResult(1L);
            }

            if (_store.TryGetValue(k, out var current) && current.Version == expectedVersion)
            {
                _store[k] = new VersionedState<AgentState>(expectedVersion + 1, state);
                Persist();
                return Task.FromResult(expectedVersion + 1);
            }

            throw new ConcurrentSessionModificationException(
                $"版本不匹配，CAS 写入失败（key={key}, 期望={expectedVersion}）。");
        }
    }

    private void Persist()
    {
        lock (_fileLock)
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var snapshot = _store.ToDictionary(kv => kv.Key, kv => new PersistedEntry
            {
                Version = kv.Value.Version,
                SessionId = kv.Value.State.SessionId,
                UserId = kv.Value.State.UserId,
                Summary = kv.Value.State.Summary,
                ReplyId = kv.Value.State.ReplyId,
                CurIter = kv.Value.State.CurIter
            });

            var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }
    }

    private void Load()
    {
        lock (_fileLock)
        {
            if (!File.Exists(_filePath))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var snapshot = JsonSerializer.Deserialize<Dictionary<string, PersistedEntry>>(json, _jsonOptions);
                if (snapshot == null)
                {
                    return;
                }

                foreach (var kv in snapshot)
                {
                    var parts = kv.Key.Split("::");
                    var state = new AgentState(kv.Value.SessionId, kv.Value.UserId)
                    {
                        Summary = kv.Value.Summary ?? "",
                        ReplyId = kv.Value.ReplyId ?? "",
                        CurIter = kv.Value.CurIter
                    };
                    _store[kv.Key] = new VersionedState<AgentState>(kv.Value.Version, state);
                }
            }
            catch
            {
                // 加载失败时忽略，保持空存储，避免阻塞启动
            }
        }
    }

    private sealed class PersistedEntry
    {
        public long Version { get; set; }
        public string SessionId { get; set; } = "";
        public string? UserId { get; set; }
        public string? Summary { get; set; }
        public string? ReplyId { get; set; }
        public int CurIter { get; set; }
    }
}
