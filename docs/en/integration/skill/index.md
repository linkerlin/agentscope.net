# Skill Repository

Skill repository extensions load skill definitions from external storage for use by Agent runtimes. AgentScope provides a three-layer skill architecture:

- **Core** (`AgentScope.Core.Skill`): `ISkill`, `ISkillRepository` (`Scan`/`Load`), `MarkdownSkillParser`, `SkillRegistry`, `SkillToolFactory`, `RegisteredSkill`
- **Harness** (`AgentScope.Harness.Skill`): `WorkspaceSkillRepository`, `SkillCatalog`, `SkillLoadTool`, `RuntimeContextSkillRepository`
- **Extensions** (`AgentScope.Extensions.Skill`): external storage implementations

## Extension implementations

| Extension | Backend | Description |
| --- | --- | --- |
| [Git Repository](git-repository.md) | Remote Git repo | Loads skills from a Git repository via LibGit2Sharp |
| [MySQL Repository](mysql-repository.md) | MySQL database | Loads skills from MySQL via MySqlConnector |
| [PostgreSQL Repository](postgresql-repository.md) | PostgreSQL database | Loads skills from PostgreSQL via Npgsql |

## Integration flow

Extension repository (implements `AgentScope.Extensions.Skill.ISkillRepository`) → Core `SkillRegistry` / Harness `SkillCatalog` → Harness `SkillLoadTool` → `HarnessAgentBuilder.WithToolkit(...)`
