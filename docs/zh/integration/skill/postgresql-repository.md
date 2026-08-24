# PostgreSQL 技能仓库

`AgentScope.Extensions.Skill.PostgreSql` 使用 **Npgsql** 从 PostgreSQL 数据库加载技能定义。

包版本：**2.0.1** | 目标框架：**net10.0**

## 何时使用

- 通过管理后台在线运营技能，希望"改完即生效"
- 已经有 PostgreSQL 基础设施

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Skill.PostgreSql" Version="2.0.1" />
</ItemGroup>
```

## 构造函数

```csharp
public PostgreSqlSkillRepository(string connectionString)
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `connectionString` | `string` | 是 | PostgreSQL 连接字符串 |

构造时自动连接数据库并执行 `CREATE TABLE IF NOT EXISTS` 确保 `skills` 表存在。

## 表结构

```sql
CREATE TABLE IF NOT EXISTS skills (
    name TEXT PRIMARY KEY,
    description TEXT,
    content TEXT NOT NULL,
    source TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

## 公开方法

| 方法 | 返回 | 说明 |
|------|------|------|
| `GetSkillAsync(string, CancellationToken)` | `Task<Skill?>` | 按名称从 `skills` 表查询技能 |
| `GetAllSkillNamesAsync(CancellationToken)` | `Task<IReadOnlyList<string>>` | 查询 `skills` 表全部 `name` |
| `SkillExistsAsync(string, CancellationToken)` | `Task<bool>` | 检查技能名称是否存在 |

每个方法独立创建和关闭数据库连接，无连接池管理要求。
