using System.Runtime.CompilerServices;
using MySqlConnector;

namespace AgentScope.Extensions.Store.MySql;

public sealed class MySqlDistributedStore : IDistributedStore
{
    private readonly string _connectionString;

    public MySqlDistributedStore(string connectionString)
    {
        _connectionString = connectionString;
        EnsureTableAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureTableAsync()
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS agentscope_store (
    `key` VARCHAR(255) PRIMARY KEY,
    `value` LONGTEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NULL
)";
        await cmd.ExecuteNonQueryAsync();
    }

    public async ValueTask<string?> GetAsync(string key, CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT `value` FROM agentscope_store WHERE `key` = @k AND (expires_at IS NULL OR expires_at > NOW())";
        cmd.Parameters.AddWithValue("@k", key);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result?.ToString();
    }

    public async ValueTask SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = ttl.HasValue
            ? "REPLACE INTO agentscope_store (`key`, `value`, `expires_at`) VALUES (@k, @v, DATE_ADD(NOW(), INTERVAL @t SECOND))"
            : "REPLACE INTO agentscope_store (`key`, `value`) VALUES (@k, @v)";
        cmd.Parameters.AddWithValue("@k", key);
        cmd.Parameters.AddWithValue("@v", value);
        if (ttl.HasValue) cmd.Parameters.AddWithValue("@t", (int)ttl.Value.TotalSeconds);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM agentscope_store WHERE `key` = @k";
        cmd.Parameters.AddWithValue("@k", key);
        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async IAsyncEnumerable<string> ListKeysAsync(string prefix, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT `key` FROM agentscope_store WHERE `key` LIKE @p";
        cmd.Parameters.AddWithValue("@p", $"{prefix}%");
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            yield return reader.GetString(0);
    }
}

