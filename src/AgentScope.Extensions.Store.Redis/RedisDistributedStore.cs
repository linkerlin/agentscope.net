using System.Runtime.CompilerServices;
using StackExchange.Redis;

namespace AgentScope.Extensions.Store.Redis;

public sealed class RedisDistributedStore : IDistributedStore, IAsyncDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisDistributedStore(string connectionString)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _db = _redis.GetDatabase();
    }

    public async ValueTask<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var val = await _db.StringGetAsync(key);
        return val.HasValue ? val.ToString() : null;
    }

    public async ValueTask SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        await _db.StringSetAsync(key, value, ttl, When.Always, CommandFlags.None);
    }

    public async ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default)
        => await _db.KeyDeleteAsync(key);

    public async IAsyncEnumerable<string> ListKeysAsync(string prefix, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
            yield return key.ToString();
    }

    public async ValueTask DisposeAsync() { await _redis.CloseAsync(); _redis.Dispose(); }
}

