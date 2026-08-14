using System.Collections.Concurrent;

namespace AgentScope.Harness;

/// <summary>
/// 分布式存储抽象。对标 Java DistributedStore。
/// 提供版本化的键值存储接口，支持乐观并发控制（CAS）。
/// </summary>
public interface IDistributedStore
{
    ValueTask<string?> GetAsync(string key, CancellationToken ct = default);
    ValueTask SetAsync(string key, string value, long? expectedVersion = null, CancellationToken ct = default);
    ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default);
    ValueTask<long> GetVersionAsync(string key, CancellationToken ct = default);
}

/// <summary>
/// 内存分布式存储。对标 Java InMemoryStore 的分布式版本。
/// </summary>
public sealed class InMemoryDistributedStore : IDistributedStore
{
    private readonly ConcurrentDictionary<string, (string Value, long Version)> _store = new();

    public ValueTask<string?> GetAsync(string key, CancellationToken ct = default) =>
        ValueTask.FromResult(_store.TryGetValue(key, out var e) ? e.Value : (string?)null);

    public ValueTask SetAsync(string key, string value, long? expectedVersion = null, CancellationToken ct = default)
    {
        if (expectedVersion.HasValue)
        {
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
            _store.AddOrUpdate(key, _ => (value, 1), (_, e) => (value, e.Version + 1));
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default) =>
        ValueTask.FromResult(_store.TryRemove(key, out _));

    public ValueTask<long> GetVersionAsync(string key, CancellationToken ct = default) =>
        ValueTask.FromResult(_store.TryGetValue(key, out var e) ? e.Version : 0L);
}
