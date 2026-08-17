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

namespace AgentScope.Harness;

/// <summary>
/// Distributed store abstraction. Counterpart to Java DistributedStore.
/// 分布式存储抽象。对标 Java DistributedStore。
/// Provides a versioned key-value storage interface with optimistic concurrency control (CAS).
/// 提供版本化的键值存储接口，支持乐观并发控制（CAS）。
/// </summary>
public interface IDistributedStore
{
    /// <summary>
    /// Gets the value associated with the specified key.
    /// 获取指定键关联的值。
    /// </summary>
    /// <param name="key">The key to look up. / 要查找的键。</param>
    /// <param name="ct">Cancellation token. / 取消令牌。</param>
    /// <returns>The value if found; otherwise null. / 找到则返回值，否则返回 null。</returns>
    ValueTask<string?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Sets the value for the specified key with optional version-based CAS.
    /// 为指定键设置值，支持可选的基于版本的乐观并发控制（CAS）。
    /// </summary>
    /// <param name="key">The key. / 键。</param>
    /// <param name="value">The value to store. / 要存储的值。</param>
    /// <param name="expectedVersion">
    /// Expected version for CAS; if provided and mismatched, an exception is thrown.
    /// CAS 期望版本号；若提供且不匹配则抛出异常。
    /// </param>
    /// <param name="ct">Cancellation token. / 取消令牌。</param>
    ValueTask SetAsync(string key, string value, long? expectedVersion = null, CancellationToken ct = default);

    /// <summary>
    /// Deletes the entry for the specified key.
    /// 删除指定键的条目。
    /// </summary>
    /// <param name="key">The key to delete. / 要删除的键。</param>
    /// <param name="ct">Cancellation token. / 取消令牌。</param>
    /// <returns>True if the entry existed and was deleted; otherwise false. / 若条目存在并已删除则为 true，否则 false。</returns>
    ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Gets the current version number for the specified key.
    /// 获取指定键的当前版本号。
    /// </summary>
    /// <param name="key">The key. / 键。</param>
    /// <param name="ct">Cancellation token. / 取消令牌。</param>
    /// <returns>The current version, or 0 if the key does not exist. / 当前版本号；若键不存在则返回 0。</returns>
    ValueTask<long> GetVersionAsync(string key, CancellationToken ct = default);
}

/// <summary>
/// In-memory distributed store. Counterpart to the distributed version of Java InMemoryStore.
/// 内存分布式存储。对标 Java InMemoryStore 的分布式版本。
/// </summary>
public sealed class InMemoryDistributedStore : IDistributedStore
{
    private readonly ConcurrentDictionary<string, (string Value, long Version)> _store = new();

    /// <inheritdoc />
    public ValueTask<string?> GetAsync(string key, CancellationToken ct = default) =>
        ValueTask.FromResult(_store.TryGetValue(key, out var e) ? e.Value : (string?)null);

    /// <inheritdoc />
    public ValueTask SetAsync(string key, string value, long? expectedVersion = null, CancellationToken ct = default)
    {
        if (expectedVersion.HasValue)
        {
            // CAS 写入：键不存在时直接抛出异常 // CAS write: throw if key does not exist
            _store.AddOrUpdate(key,
                _ => throw new InvalidOperationException($"键 {key} 不存在"),
                (_, existing) =>
                {
                    if (existing.Version != expectedVersion.Value)
                        throw new InvalidOperationException($"版本冲突: 期望 {expectedVersion.Value}, 实际 {existing.Version}");
                    return (value, existing.Version + 1);
                });
        }
        else
        {
            // 非 CAS 写入：键不存在则版本从 1 开始 // Non-CAS write: start version at 1 if key is new
            _store.AddOrUpdate(key, _ => (value, 1), (_, e) => (value, e.Version + 1));
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default) =>
        ValueTask.FromResult(_store.TryRemove(key, out _));

    /// <inheritdoc />
    public ValueTask<long> GetVersionAsync(string key, CancellationToken ct = default) =>
        ValueTask.FromResult(_store.TryGetValue(key, out var e) ? e.Version : 0L);
}
