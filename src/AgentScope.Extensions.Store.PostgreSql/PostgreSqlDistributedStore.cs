using System.Runtime.CompilerServices;
using Npgsql;

namespace AgentScope.Extensions.Store.PostgreSql;

public sealed class PostgreSqlDistributedStore : IDistributedStore
{
    private readonly string _connectionString;

    public PostgreSqlDistributedStore(string connectionString)
    {
        _connectionString = connectionString;
        EnsureTableAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureTableAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS agentscope_store (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    expires_at TIMESTAMPTZ NULL
)";
        await cmd.ExecuteNonQueryAsync();
    }

    public async ValueTask<string?> GetAsync(string key, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT value FROM agentscope_store WHERE key = @k AND (expires_at IS NULL OR expires_at > NOW())", conn);
        cmd.Parameters.AddWithValue("k", key);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result?.ToString();
    }

    public async ValueTask SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(@"
INSERT INTO agentscope_store (key, value, expires_at)
VALUES (@k, @v, @t)
ON CONFLICT (key) DO UPDATE SET value = @v, expires_at = @t", conn);
        cmd.Parameters.AddWithValue("k", key);
        cmd.Parameters.AddWithValue("v", value);
        cmd.Parameters.AddWithValue("t", ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("DELETE FROM agentscope_store WHERE key = @k", conn);
        cmd.Parameters.AddWithValue("k", key);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async IAsyncEnumerable<string> ListKeysAsync(string prefix, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT key FROM agentscope_store WHERE key LIKE @p", conn);
        cmd.Parameters.AddWithValue("p", $"{prefix}%");
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            yield return reader.GetString(0);
    }
}

