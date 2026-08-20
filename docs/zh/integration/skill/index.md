# 技能仓库（Skill Repository）

技能仓库扩展负责从外部存储加载技能定义，供 Agent 运行时使用。AgentScope 提供了三层技能架构：

- **Core 层**（`AgentScope.Core.Skill`）：`ISkill`、`ISkillRepository`（`Scan`/`Load`）、`MarkdownSkillParser`、`SkillRegistry`、`SkillToolFactory`、`RegisteredSkill`
- **Harness 层**（`AgentScope.Harness.Skill`）：`WorkspaceSkillRepository`、`SkillCatalog`、`SkillLoadTool`、`RuntimeContextSkillRepository`
- **Extensions 层**（`AgentScope.Extensions.Skill`）：对外部存储的技能仓库实现

## 扩展实现

| 扩展 | 后端 | 说明 |
| --- | --- | --- |
| [Git 仓库](git-repository.md) | 远程 Git 仓库 | 使用 LibGit2Sharp 从 Git 仓库加载技能 |
| [MySQL 仓库](mysql-repository.md) | MySQL 数据库 | 使用 MySqlConnector 从 MySQL 加载技能 |
| [PostgreSQL 仓库](postgresql-repository.md) | PostgreSQL 数据库 | 使用 Npgsql 从 PostgreSQL 加载技能 |

## 集成方式

扩展仓库（实现 `AgentScope.Extensions.Skill.ISkillRepository`）→ Core `SkillRegistry` / Harness `SkillCatalog` → Harness `SkillLoadTool` → `HarnessAgentBuilder.WithToolkit(...)`
