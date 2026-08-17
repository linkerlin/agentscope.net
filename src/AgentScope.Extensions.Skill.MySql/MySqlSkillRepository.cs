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

using MySqlConnector;

namespace AgentScope.Extensions.Skill.MySql;

public sealed class MySqlSkillRepository : ISkillRepository
{
    private readonly string _connectionString;

    public MySqlSkillRepository(string connectionString)
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
CREATE TABLE IF NOT EXISTS skills (
    name VARCHAR(255) PRIMARY KEY,
    description TEXT,
    content LONGTEXT NOT NULL,
    source VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
)";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Skill?> GetSkillAsync(string name, CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name, description, content, source FROM skills WHERE name = @n";
        cmd.Parameters.AddWithValue("@n", name);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
            return new Skill(reader.GetString(0), reader.IsDBNull(1) ? "" : reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3));
        return null;
    }

    public async Task<IReadOnlyList<string>> GetAllSkillNamesAsync(CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM skills ORDER BY name";
        var names = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) names.Add(reader.GetString(0));
        return names;
    }

    public async Task<bool> SkillExistsAsync(string name, CancellationToken ct = default)
    {
        await using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM skills WHERE name = @n";
        cmd.Parameters.AddWithValue("@n", name);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) > 0;
    }
}
