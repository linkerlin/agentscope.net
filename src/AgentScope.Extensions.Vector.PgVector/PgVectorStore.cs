using System.Data.Common;
using System.Runtime.CompilerServices;
using AgentScope.Extensions.Vector;
using Npgsql;

namespace AgentScope.Extensions.Vector.PgVector;

/// <summary>
/// PostgreSQL pgvector 向量存储适配器。对标 Java PgVectorStore。
/// </summary>
public sealed class PgVectorStore(NpgsqlDataSource dataSource, string tableName, int dimension) : IVectorStore
{
    public int Dimension => dimension;

    public async ValueTask UpsertAsync(string collection, string id, float[] vector,
        IDictionary<string, object>? payload = null, CancellationToken ct = default)
    {
        await using var cmd = dataSource.CreateCommand(
            $@"INSERT INTO {tableName} (id, vector, payload, created_at)
               VALUES ($1, $2::vector, $3::jsonb, NOW())
               ON CONFLICT (id) DO UPDATE SET vector = $2::vector, payload = $3::jsonb");
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(vector);
        cmd.Parameters.AddWithValue(System.Text.Json.JsonSerializer.Serialize(payload ?? new Dictionary<string, object>()));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async IAsyncEnumerable<SearchHit> SearchAsync(string collection, float[] query,
        int topK = 5, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var cmd = dataSource.CreateCommand(
            $@"SELECT id, 1 - (vector <=> $1::vector) AS score, payload
               FROM {tableName}
               ORDER BY vector <=> $1::vector
               LIMIT $2");
        cmd.Parameters.AddWithValue(query);
        cmd.Parameters.AddWithValue(topK);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            yield return new SearchHit(
                reader.GetString(0),
                (float)reader.GetDouble(1));
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
