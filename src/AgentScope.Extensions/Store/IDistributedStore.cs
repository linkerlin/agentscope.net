namespace AgentScope.Extensions.Store;

/// <summary>
/// 分布式存储接口。对标 Java BaseStore。
/// </summary>
public interface IDistributedStore
{
    ValueTask<string?> GetAsync(string key, CancellationToken ct = default);
    ValueTask SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default);
    ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default);
    IAsyncEnumerable<string> ListKeysAsync(string prefix, CancellationToken ct = default);
}
