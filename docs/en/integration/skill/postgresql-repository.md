# PostgreSQL Skill Repository

`AgentScope.Extensions.Skill.PostgreSql` uses **Npgsql** to load skill definitions from a PostgreSQL database.

Package version: **2.0.1** | Target framework: **net10.0**

## When to use

- You operate skills via an admin console and want changes to take effect immediately
- You already have PostgreSQL infrastructure

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Skill.PostgreSql" Version="2.0.1" />
</ItemGroup>
```

## Constructor

```csharp
public PostgreSqlSkillRepository(string connectionString)
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `connectionString` | `string` | Yes | PostgreSQL connection string |

On construction, the repository connects and runs `CREATE TABLE IF NOT EXISTS` to ensure the `skills` table exists.

## Schema

```sql
CREATE TABLE IF NOT EXISTS skills (
    name TEXT PRIMARY KEY,
    description TEXT,
    content TEXT NOT NULL,
    source TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

## Public methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetSkillAsync(string, CancellationToken)` | `Task<Skill?>` | Queries a skill by name from `skills` table |
| `GetAllSkillNamesAsync(CancellationToken)` | `Task<IReadOnlyList<string>>` | Lists all skill names from `skills` table |
| `SkillExistsAsync(string, CancellationToken)` | `Task<bool>` | Checks if a skill name exists |

Each method creates and closes its own database connection — no connection pool management required.
