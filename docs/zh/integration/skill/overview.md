# 技能仓库概述

`AgentScope.Extensions.Skill.ISkillRepository` 接口定义了从外部存储加载技能的异步方法：

```csharp
public interface ISkillRepository
{
    Task<Skill?> GetSkillAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAllSkillNamesAsync(CancellationToken ct = default);
    Task<bool> SkillExistsAsync(string name, CancellationToken ct = default);
}
```

返回的 `Skill` 是一个不可变记录：

```csharp
public sealed record Skill(string Name, string Description, string Content, string? Source = null);
```

> **Core 层**另有 `AgentScope.Core.Skill.ISkillRepository`（`Scan()` / `Load(RegisteredSkill)`），两者不同。Extensions 层仓库实现了 `AgentScope.Extensions.Skill.ISkillRepository`，可与 Core 层的 `SkillRegistry` 配合使用。

## 可用实现

| 扩展 | 命名空间 | 构造函数 |
| --- | --- | --- |
| [Git 仓库](git-repository.md) | `AgentScope.Extensions.Skill.Git` | `(string repoUrl, string branch = "main", string? localPath = null)` |
| [MySQL 仓库](mysql-repository.md) | `AgentScope.Extensions.Skill.MySql` | `(string connectionString)` |
| [PostgreSQL 仓库](postgresql-repository.md) | `AgentScope.Extensions.Skill.PostgreSql` | `(string connectionString)` |

## 技能文件格式

技能由 `MarkdownSkillParser`（`AgentScope.Core.Skill`）解析，格式为 YAML front matter + Markdown 正文：

```markdown
---
name: my-skill
description: 一个示例技能
tools: [tool1, tool2]
active: true
---

# 技能正文

在此编写技能的详细说明……
```

解析结果返回 `RegisteredSkill`，包含 `Id`、`Name`、`Description`、`ToolNames`、`IsActiveByDefault`、`RawContent` 等属性。

## 集成到 Harness

1. 创建扩展仓库实例（如 `GitSkillRepository`）
2. 通过 `SkillRegistry` 注册或通过 Harness `SkillCatalog` 构建快照
3. `SkillLoadTool` 接收 `HarnessSkillEntry` 列表，暴露为 `load_skill` 工具
4. 通过 `HarnessAgentBuilder.WithToolkit(...)` 将工具注入 Agent
