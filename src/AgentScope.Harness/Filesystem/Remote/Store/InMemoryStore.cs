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

using System.Collections.Concurrent;

namespace AgentScope.Harness.Filesystem.Remote.Store;

/// <summary>
/// 基于内存并发字典的 KV 存储（IBaseStore 实现），用于测试与单机场景。
/// 对应 Java: io.agentscope.harness.agent.filesystem.remote.store.InMemoryStore
/// </summary>
public sealed class InMemoryStore : IBaseStore
{
    private readonly ConcurrentDictionary<string, StoreItem> _items = new();

    public Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        _items.TryGetValue(key, out var item);
        return Task.FromResult(item?.Value);
    }

    public Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        _items.AddOrUpdate(key,
            _ => new StoreItem { Key = key, Value = value, CreatedAt = now, UpdatedAt = now },
            (_, existing) =>
            {
                existing.Value = value;
                existing.Touch();
                return existing;
            });
        return Task.CompletedTask;
    }

    public Task<bool> DeleteAsync(string key, CancellationToken ct = default)
        => Task.FromResult(_items.TryRemove(key, out _));

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => Task.FromResult(_items.ContainsKey(key));

    /// <summary>返回所有键的快照（测试/调试用）。</summary>
    public IReadOnlyCollection<string> ListKeys() => _items.Keys.ToArray();
}
