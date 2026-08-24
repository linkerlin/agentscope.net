# Skill Repository Overview

`AgentScope.Extensions.Skill.ISkillRepository` defines the async contract for loading skills from external storage:

```csharp
public interface ISkillRepository
{
    Task<Skill?> GetSkillAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAllSkillNamesAsync(CancellationToken ct = default);
    Task<bool> SkillExistsAsync(string name, CancellationToken ct = default);
}
```

The return type `Skill` is an immutable record:

```csharp
public sealed record Skill(string Name, string Description, string Content, string? Source = null);
```

> **Core** defines a separate `AgentScope.Core.Skill.ISkillRepository` (`Scan()` / `Load(RegisteredSkill)`). The Extensions repository (`AgentScope.Extensions.Skill.ISkillRepository`) can be used with Core's `SkillRegistry`.

## Available implementations

| Extension | Namespace | Constructor |
| --- | --- | --- |
| [Git Repository](git-repository.md) | `AgentScope.Extensions.Skill.Git` | `(string repoUrl, string branch = "main", string? localPath = null)` |
| [MySQL Repository](mysql-repository.md) | `AgentScope.Extensions.Skill.MySql` | `(string connectionString)` |
| [PostgreSQL Repository](postgresql-repository.md) | `AgentScope.Extensions.Skill.PostgreSql` | `(string connectionString)` |

## Skill file format

Skills are parsed by `MarkdownSkillParser` (`AgentScope.Core.Skill`) from YAML front matter + Markdown body:

```markdown
---
name: my-skill
description: An example skill
tools: [tool1, tool2]
active: true
---

# Skill Body

Detailed instructions go here...
```

The parser returns a `RegisteredSkill` with `Id`, `Name`, `Description`, `ToolNames`, `IsActiveByDefault`, and `RawContent`.

## Harness integration

1. Create an extension repository (e.g. `GitSkillRepository`)
2. Register via `SkillRegistry` or build a `SkillCatalog` snapshot
3. `SkillLoadTool` accepts `HarnessSkillEntry` items, exposed as the `load_skill` tool
4. Inject into Agent via `HarnessAgentBuilder.WithToolkit(...)`
