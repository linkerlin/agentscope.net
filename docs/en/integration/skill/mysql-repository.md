# MySQL Skill Repository

`AgentScope.Extensions.Skill.MySql` uses **MySqlConnector** to load skill definitions from a MySQL database.

Package version: **2.0.1** | Target framework: **net10.0**

## When to use

- You operate skills via an admin console and want changes to take effect immediately
- You already have MySQL infrastructure

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Skill.MySql" Version="2.0.1" />
</ItemGroup>
```

## Constructor

```csharp
public MySqlSkillRepository(string connectionString)
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `connectionString` | `string` | Yes | MySQL connection string |

On construction, the repository connects and runs `CREATE TABLE IF NOT EXISTS` to ensure the `skills` table exists.

## Schema

```sql
CREATE TABLE IF NOT EXISTS skills (
    name VARCHAR(255) PRIMARY KEY,
    description TEXT,
    content LONGTEXT NOT NULL,
    source VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Public methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetSkillAsync(string, CancellationToken)` | `Task<Skill?>` | Queries a skill by name from `skills` table |
| `GetAllSkillNamesAsync(CancellationToken)` | `Task<IReadOnlyList<string>>` | Lists all skill names from `skills` table |
| `SkillExistsAsync(string, CancellationToken)` | `Task<bool>` | Checks if a skill name exists |

Each method creates and closes its own database connection — no connection pool management required.
